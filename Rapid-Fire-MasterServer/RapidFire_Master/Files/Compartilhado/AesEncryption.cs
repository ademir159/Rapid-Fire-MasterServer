using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class AesEncryption
{
    public static string Encrypt(string plainText, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        keyBytes = FixKeyLength(keyBytes, 32); // AES-256 = 32 bytes

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.GenerateIV(); // Gera IV aleatório

            using (var ms = new MemoryStream())
            {
                // Escreve IV no início
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string Decrypt(string encryptedBase64, string key)
    {
        byte[] fullData = Convert.FromBase64String(encryptedBase64);
        byte[] iv = new byte[16];
        byte[] cipherText = new byte[fullData.Length - iv.Length];

        Array.Copy(fullData, 0, iv, 0, iv.Length);
        Array.Copy(fullData, iv.Length, cipherText, 0, cipherText.Length);

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        keyBytes = FixKeyLength(keyBytes, 32);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = iv;

            using (var ms = new MemoryStream(cipherText))
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }

    // Garante que a chave tenha o tamanho necessário (32 bytes para AES-256)
    private static byte[] FixKeyLength(byte[] keyBytes, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(keyBytes, result, Math.Min(keyBytes.Length, length));
        return result;
    }
}
