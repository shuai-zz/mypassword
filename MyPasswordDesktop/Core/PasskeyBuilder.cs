using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Rpc;
using MyPasswordDesktop.Rpc.Request;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Core
{
    /// <summary>
    /// Builds a WebAuthn public-key credential (ES256, P-256) for a passkey
    /// registration ceremony proxied from the browser extension.
    /// </summary>
    public static class PasskeyBuilder
    {
        public const int CoseAlgEs256 = -7;

        private static readonly byte[] Aaguid = new byte[16];

        private const int FlagUp = 0x01; // User Present
        private const int FlagUv = 0x04; // User Verified
        private const int FlagAt = 0x40; // Attested credential data included

        public sealed class Result
        {
            public PasskeyData Data;
            public byte[] CredentialId;
            public byte[] ClientDataJson;
            public byte[] AuthenticatorData;
            public byte[] AttestationObject;
            public byte[] PublicKeySpki;
            public int PublicKeyAlgorithm;
        }

        public static Result Build(PasskeyAddRequest req)
        {
            if (req.options?.rp == null || req.options.user == null)
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Missing passkey options.");
            }
            string rpId = req.options.rp.id;
            StringUtils.CheckNotEmpty("rpId", rpId);
            StringUtils.CheckNotEmpty("origin", req.origin);
            ValidateOrigin(req.origin, rpId);
            RequireEs256(req);

            try
            {
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                ECParameters pub = ecdsa.ExportParameters(false);
                byte[] x = pub.Q.X;
                byte[] y = pub.Q.Y;
                var rawPub = new byte[65];
                rawPub[0] = 0x04;
                Buffer.BlockCopy(x, 0, rawPub, 1, 32);
                Buffer.BlockCopy(y, 0, rawPub, 33, 32);

                byte[] pkcs8 = ecdsa.ExportPkcs8PrivateKey();
                byte[] credentialId = EncryptUtils.GenerateSecureRandomBytes(16);

                byte[] cosePubKey = BuildCoseEc2PublicKey(x, y);
                byte[] authData = BuildAuthenticatorData(rpId, credentialId, cosePubKey);
                byte[] clientDataJson = BuildClientDataJson("webauthn.create", req.options.challenge, req.origin);

                var att = new CborWriter();
                att.WriteMapHeader(3);
                att.WriteText("fmt").WriteText("none");
                att.WriteText("attStmt").WriteMapHeader(0);
                att.WriteText("authData").WriteBytes(authData);
                byte[] attestationObject = att.ToByteArray();

                var pd = new PasskeyData
                {
                    relyingPartyId = rpId,
                    relyingPartyName = req.options.rp.name,
                    b64UserId = req.options.user.id,
                    username = req.options.user.name,
                    displayName = req.options.user.displayName,
                    alg = CoseAlgEs256,
                    b64CredentialId = Base64Utils.B64(credentialId),
                    b64PubKey = Base64Utils.B64(rawPub),
                    b64PrivKey = Base64Utils.B64(pkcs8),
                    createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };

                return new Result
                {
                    Data = pd,
                    CredentialId = credentialId,
                    ClientDataJson = clientDataJson,
                    AuthenticatorData = authData,
                    AttestationObject = attestationObject,
                    PublicKeySpki = ecdsa.ExportSubjectPublicKeyInfo(),
                    PublicKeyAlgorithm = CoseAlgEs256,
                };
            }
            catch (CryptographicException e)
            {
                throw new EncryptException(e);
            }
        }

        /// <summary>The origin's host must equal rpId or be a sub-domain of rpId.</summary>
        public static void ValidateOrigin(string origin, string rpId)
        {
            string host;
            try
            {
                host = new Uri(origin).Host;
            }
            catch (Exception)
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Invalid origin: " + origin);
            }
            if (string.IsNullOrEmpty(host))
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Invalid origin: " + origin);
            }
            if (!host.Equals(rpId) && !host.EndsWith("." + rpId))
            {
                throw new VaultException(ErrorCode.BAD_REQUEST,
                    $"Origin host '{host}' does not match rp.id '{rpId}'.");
            }
        }

        private static void RequireEs256(PasskeyAddRequest req)
        {
            var prams = req.options.pubKeyCredParams;
            if (prams == null || prams.Length == 0)
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "No pubKeyCredParams.");
            }
            foreach (var p in prams)
            {
                if (p.alg == CoseAlgEs256)
                {
                    return;
                }
            }
            throw new VaultException(ErrorCode.BAD_REQUEST, "RP does not accept ES256.");
        }

        private static byte[] BuildCoseEc2PublicKey(byte[] x, byte[] y)
        {
            var w = new CborWriter();
            w.WriteMapHeader(5);
            w.WriteInt(1).WriteInt(2);   // kty = EC2
            w.WriteInt(3).WriteInt(-7);  // alg = ES256
            w.WriteInt(-1).WriteInt(1);  // crv = P-256
            w.WriteInt(-2).WriteBytes(x);
            w.WriteInt(-3).WriteBytes(y);
            return w.ToByteArray();
        }

        private static byte[] BuildAuthenticatorData(string rpId, byte[] credentialId, byte[] cosePubKey)
        {
            byte[] rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));
            using var output = new MemoryStream();
            output.Write(rpIdHash, 0, 32);
            output.WriteByte(FlagUp | FlagUv | FlagAt);
            output.Write([0, 0, 0, 0], 0, 4); // signCount = 0
            output.Write(Aaguid, 0, 16);
            int len = credentialId.Length;
            output.WriteByte((byte)((len >> 8) & 0xff));
            output.WriteByte((byte)(len & 0xff));
            output.Write(credentialId, 0, credentialId.Length);
            output.Write(cosePubKey, 0, cosePubKey.Length);
            return output.ToArray();
        }

        /// <summary>
        /// Build a WebAuthn <c>clientDataJSON</c>. <paramref name="type"/> is
        /// <c>webauthn.create</c> or <c>webauthn.get</c>.
        /// </summary>
        public static byte[] BuildClientDataJson(string type, string challengeB64, string origin)
        {
            var data = new ClientData
            {
                type = type,
                challenge = challengeB64 ?? "",
                origin = origin,
                crossOrigin = false,
            };
            return Encoding.UTF8.GetBytes(JsonUtils.ToJson(data));
        }
    }
}
