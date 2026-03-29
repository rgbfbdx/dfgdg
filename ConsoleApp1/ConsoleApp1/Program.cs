using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter directory path (leave empty for current directory):");
            var dir = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(dir)) dir = Directory.GetCurrentDirectory();

            if (!Directory.Exists(dir))
            {
                Console.WriteLine("Directory does not exist.");
                return;
            }

            var files = Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories);
            Console.WriteLine($"Found {files.Length} .txt files.");
            if (files.Length == 0) return;

            var sw = Stopwatch.StartNew();

            // Используем Task[] + TaskCompletionSource, чтобы точно знать, когда все фоновые задачи завершены
            var tasks = new Task[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                var path = files[i];
                var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                tasks[i] = tcs.Task;

                // Как только найден — сразу ставим задачу в ThreadPool
                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    try
                    {
                        Console.WriteLine($"Start encrypting: {path}");

                        // Считываем синхронно, как в условии
                        var content = File.ReadAllText(path);

                        // Простая шифровка (Caesar)
                        var encrypted = SimpleCaesarEncrypt(content, shift: 3);

                        var outPath = path + ".enc.txt";

                        // Асинхронная запись
                        await File.WriteAllTextAsync(outPath, encrypted).ConfigureAwait(false);

                        Console.WriteLine($"Finished encrypting: {path} -> {outPath}");

                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {path}: {ex.Message}");
                        tcs.SetException(ex);
                    }
                });
            }

            // Ждём завершения всех задач
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
                // Игнорируем отдельные ошибки — они уже выведены в консоль
            }

            sw.Stop();
            Console.WriteLine($"Processed files: {files.Length}");
            Console.WriteLine($"Total time: {sw.Elapsed}");
        }

        static string SimpleCaesarEncrypt(string input, int shift)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)(chars[i] + shift);
            return new string(chars);
        }
    }
}
