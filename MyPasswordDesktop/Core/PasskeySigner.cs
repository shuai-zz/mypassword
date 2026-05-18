using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Rpc;
using MyPasswordDesktop.Rpc.Request;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktop.Core
{
    /// <summary>
    /// Signs a WebAuthn assertion with a stored passkey — the
    /// <c>navigator.credentials.get()</c> side of the ceremony.
    /// </summary>
    public static class PasskeySigner
    {
        private const int FlagUp = 0x01; // User Present
        private const int FlagUv = 0x04; // User Verified

        public sealed class Result
        {
            public byte[] CredentialId;
            public byte[] ClientDataJson;
            public byte[] AuthenticatorData;
            public byte[] SignatureDer;
            public byte[] UserHandle;
        }

        public static Result Sign(LoginItemData login, PasskeyLoginRequest req)
        {
            if (login?.data?.passkey == null)
            {
                throw new VaultException(ErrorCode.DATA_NOT_FOUND, "Login item has no passkey.");
            }
            if (req.options == null)
            {
                throw new VaultException(ErrorCode.BAD_REQUEST, "Missing passkey options.");
            }
            string rpId = req.options.rpId;
            StringUtils.CheckNotEmpty("rpId", rpId);
            StringUtils.CheckNotEmpty("origin", req.origin);
            PasskeyBuilder.ValidateOrigin(req.origin, rpId);

            PasskeyData pk = login.data.passkey;
            if (!rpId.Equals(pk.relyingPartyId))
            {
                throw new VaultException(ErrorCode.BAD_REQUEST,
                    $"RP id mismatch: stored='{pk.relyingPartyId}' requested='{rpId}'");
            }

            byte[] credentialId = Base64Utils.B64(pk.b64CredentialId);
            var allow = req.options.allowCredentials;
            if (allow != null && allow.Length > 0)
            {
                bool found = false;
                foreach (var c in allow)
                {
                    if (c?.id == null)
                    {
                        continue;
                    }
                    if (Base64Utils.B64(c.id).SequenceEqual(credentialId))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    throw new VaultException(ErrorCode.DATA_NOT_FOUND,
                        "Stored passkey not listed in allowCredentials.");
                }
            }

            try
            {
                byte[] pkcs8 = Base64Utils.B64(pk.b64PrivKey);
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportPkcs8PrivateKey(pkcs8, out _);

                byte[] clientDataJson = PasskeyBuilder.BuildClientDataJson(
                    "webauthn.get", req.options.challenge, req.origin);
                byte[] authData = BuildAssertionAuthenticatorData(rpId);

                byte[] clientDataHash = SHA256.HashData(clientDataJson);
                var toSign = new byte[authData.Length + clientDataHash.Length];
                Buffer.BlockCopy(authData, 0, toSign, 0, authData.Length);
                Buffer.BlockCopy(clientDataHash, 0, toSign, authData.Length, clientDataHash.Length);

                // ES256 → ASN.1 DER ECDSA signature, as WebAuthn expects.
                byte[] signatureDer = ecdsa.SignData(toSign, HashAlgorithmName.SHA256,
                    DSASignatureFormat.Rfc3279DerSequence);

                var r = new Result
                {
                    CredentialId = credentialId,
                    ClientDataJson = clientDataJson,
                    AuthenticatorData = authData,
                    SignatureDer = signatureDer,
                };
                if (!string.IsNullOrEmpty(pk.b64UserId))
                {
                    r.UserHandle = Base64Utils.B64(pk.b64UserId);
                }
                return r;
            }
            catch (CryptographicException e)
            {
                throw new EncryptException(e);
            }
        }

        private static byte[] BuildAssertionAuthenticatorData(string rpId)
        {
            byte[] rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));
            using var output = new MemoryStream();
            output.Write(rpIdHash, 0, 32);
            output.WriteByte(FlagUp | FlagUv);
            output.Write([0, 0, 0, 0], 0, 4); // signCount = 0
            return output.ToArray();
        }
    }
}
