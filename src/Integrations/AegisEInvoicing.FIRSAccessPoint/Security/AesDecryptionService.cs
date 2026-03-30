using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AegisEInvoicing.FIRSAccessPoint.Security;

public sealed class AesDecryptionService
{
    public static string DecryptData(string encryptedData, string ivHex, string key, string apiKey)
    {
        try
        {
            // Convert hex IV to bytes
            byte[] iv = HexStringToByteArray(ivHex);

            // Decode Base64Url encoded ciphertext directly
            byte[] encryptedBytes = DecodeBase64Url(encryptedData);

            // Convert key to bytes
            byte[] keyBytes = Encoding.UTF8.GetBytes(ConstructDecryptionKey(key, apiKey));

            // Ensure key is proper length (16, 24, or 32 bytes for AES)
            keyBytes = AdjustKeyLength(keyBytes, 32); // 256-bit key

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CFB; // Changed from CBC to CFB to match Java
                aes.Padding = PaddingMode.None; // Changed from PKCS7 to None to match Java

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Decryption failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Decode Base64Url encoded string to byte array
    /// </summary>
    private static byte[] DecodeBase64Url(string base64Url)
    {
        // Replace Base64Url characters with standard Base64 characters
        string base64 = base64Url.Replace('-', '+').Replace('_', '/');

        // Add padding if necessary
        int padding = 4 - (base64.Length % 4);
        if (padding < 4)
        {
            base64 += new string('=', padding);
        }

        return Convert.FromBase64String(base64);
    }

    private static byte[] HexStringToByteArray(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }

    private static byte[] AdjustKeyLength(byte[] key, int targetLength)
    {
        if (key.Length == targetLength)
            return key;

        byte[] adjustedKey = new byte[targetLength];
        if (key.Length < targetLength)
        {
            // Pad with zeros or repeat the key
            Array.Copy(key, adjustedKey, key.Length);
        }
        else
        {
            // Truncate
            Array.Copy(key, adjustedKey, targetLength);
        }
        return adjustedKey;
    }

    /// <summary>
    /// Constructs the decryption key by combining pub and first UUID segment
    /// </summary>
    private static string ConstructDecryptionKey(string pub, string apiKey)
    {
        string[] segments = apiKey.Split('-');
        if (segments.Length == 0 || string.IsNullOrWhiteSpace(segments[0]))
        {
            throw new ArgumentException("Invalid API key format. Expected UUID format with '-' separators.", nameof(apiKey));
        }
        return $"{pub}{segments[0]}";
    }
}


public sealed class AesDecryptionServiceAlt
{
    public static string Decrypt(byte[] key, byte[] iv,string base64UrlEncodedCiphertext)
    {
        // Convert base64url to standard base64
        string base64Ciphertext = ConvertBase64UrlToBase64(base64UrlEncodedCiphertext);

        // Decode from base64
        byte[] ciphertext = Convert.FromBase64String(base64Ciphertext);

        // Setup AES-256-CFB decryption using BouncyCastle
        var cipher = new BufferedBlockCipher(new CfbBlockCipher(new AesEngine(), 128)); // 128-bit feedback
        cipher.Init(false, new ParametersWithIV(new KeyParameter(key), iv)); // false for decryption

        // Decrypt
        byte[] output = new byte[cipher.GetOutputSize(ciphertext.Length)];
        int length = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, output, 0);
        cipher.DoFinal(output, length);

        // Remove padding manually if needed
        int padding = output[^1];
        if (padding > 0 && padding <= 16)
        {
            // Check if it's valid PKCS7 padding
            bool validPadding = true;
            for (int i = output.Length - padding; i < output.Length; i++)
            {
                if (output[i] != padding)
                {
                    validPadding = false;
                    break;
                }
            }
            if (validPadding)
            {
                byte[] unpadded = new byte[output.Length - padding];
                Array.Copy(output, 0, unpadded, 0, unpadded.Length);
                return Encoding.UTF8.GetString(unpadded);
            }
        }

        var jsonString = Encoding.UTF8.GetString(output);
        using var doc = JsonDocument.Parse(jsonString);
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string ConvertBase64UrlToBase64(string base64Url)
    {
        string base64 = base64Url.Replace('-', '+').Replace('_', '/');

        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return base64;
    }

    public static byte[] HexStringToByteArray(string hex)
    {
        int len = hex.Length;
        byte[] data = new byte[len / 2];
        for (int i = 0; i < len; i += 2)
        {
            data[i / 2] = (byte)((Convert.ToInt32(hex[i].ToString(), 16) << 4)
                               + Convert.ToInt32(hex[i + 1].ToString(), 16));
        }
        return data;
    }
}

