using System;
using System.Globalization;
using System.Security.Cryptography;


class Brut
{
    private SHA256 _sha256;
    private byte[] _hash;
    private List<byte[]> _hashesFromFile;
    private List<string> _hashesFromFileString;

    public Brut()
    {
        _sha256 = SHA256.Create();
    }

    // Сравнение символьных массивов.
    private bool CheckByteArray(byte[] array1, byte[] array2)
    {
        if (array1.Length != array2.Length) return false;
        for (int i = 0; i < array1.Length; i++)
            if (array1[i] != array2[i])
                return false;
        return true;
    }

    // Возвращает хэш строки.
    private byte[] makeHash(byte[] input)
    {
        return _sha256.ComputeHash(input);
    }


    // Брутфорс.
    // streamsCount - кол-во задействованных потоков,
    // passLength - количество символов для брутфорса.
    public void Hack(int streamsCount, int passLength)
    {
        // Ставит ограничение на размер массива и количество потоков.
        if (streamsCount > 4 || streamsCount < 1 || passLength < 1)
        {
            Console.WriteLine("Error. Counts of threads must be <= 4 or >= 1.");
            throw new Exception();
        }

        bool trigger = true;

        // Создает для каждого потока начальную и конечную
        // последовательности символов в массиве.
        byte[][] combo = new byte[streamsCount][];
        byte[][] endCombo = new byte[streamsCount][];
        for (int i = 0; i < streamsCount; i++)
        {
            combo[i] = new byte[passLength];
            endCombo[i] = new byte[passLength];
        }

        for (int j = 0; j < streamsCount; j++)
        {
            for (int i = 0; i < passLength; i++)
            {
                // Пример массивов для streamsCount = 3 и passLength = 3
                /*
                 * a a a
                 * i a a
                 * q a a
                 *
                 * h z {
                 * p z {
                 * z z {
                 * 
                 */

                if (i == 0)
                {
                    combo[j][i] = (byte)('a' + (j * (26 / streamsCount)));
                    endCombo[j][i] = (byte)('a' + (j * (26 / streamsCount) - 1) + 26 / streamsCount);
                }
                else
                {
                    combo[j][i] = (byte)'a';
                    endCombo[j][i] = (byte)'z';
                }
            }

            endCombo[j][passLength - 1] = (byte)'{';
        }
        endCombo[streamsCount - 1][0] = (byte)'z';

        // Создание нескольких потоков для выполнения брутфорса.
        // Алгоритм перебирает наборы символов в массиве combo.
        // endCombo используется для предела перебора.
        for (int t = 0; t < streamsCount; t++)
        {
            Thread thread = new Thread(() =>
            {
                // Работает пока триггер равен true 
                // Исправить, так как не будет работать в многопотоке
                int k = t;
                while (true)
                {

                    for (int i = 1; i < passLength; i++)
                    {
                        if (combo[k][passLength - i] == '{')
                        {
                            // Сравнение текущего combo с endCombo и выход из перебора при совпадении.
                            if (CheckByteArray(combo[k], endCombo[k])) return;
                            combo[k][passLength - i] = (byte)'a';
                            combo[k][passLength - i - 1]++;

                        }
                    }
                    // Сравнение хэшей.
                    _hash = makeHash(combo[k]);
                    for (int i = 0; i < _hashesFromFile.Count; i++)
                    {
                        if (CheckByteArray(_hash, _hashesFromFile[i]))
                        {
                            Console.Write(_hashesFromFileString[i] + ": ");
                            trigger = false;
                            printArray(combo[k]);
                        }
                    }
                    combo[k][passLength - 1]++;
                }
            });

            thread.Start();
            thread.Join();
            Thread.Sleep(100);

        }

        if (trigger) Console.WriteLine("Nothing found");

    }

    // Вывод массива.
    public void printArray(byte[] array)
    {
        for (int i = 0; i < array.Length; i++)
            Console.Write(((char)array[i]) + " ");
        Console.WriteLine();
    }


    // Считывание хэшей из файла.
    public void ReadHashesFromFile(string filePath)
    {
        List<string> temp = new List<string>();

        StreamReader fr = new StreamReader(new FileStream(filePath, FileMode.Open));
        while (!fr.EndOfStream)
        {
            temp.Add(fr.ReadLine());
        }

        fr.Close();

        _hashesFromFileString = temp;
        HexStringToByteArray(temp);

    }

    // Считывание хэшей через консоль.
    public void ReadHashesFromConsole()
    {
        List<string> temp = new List<string>();

        while (true)
        {
            Console.Write("Enter hash code (Enter \"0\" to exit): ");
            string ans = Console.ReadLine();
            if (ans == "0")
            {
                _hashesFromFileString = temp;
                HexStringToByteArray(temp);
                return;
            }
            temp.Add(ans);
        }
    }

    // Преобразование типа хэшей из HexString в byte[].
    private void HexStringToByteArray(List<string> hexStringHash)
    {
        List<byte[]> temp = new List<byte[]>();
        for (int i = 0; i < hexStringHash.Count; i++)
        {
            var bytes = new byte[hexStringHash[i].Length / 2];

            for (int j = 0; j < bytes.Length; j++)
            {
                bytes[j] = byte.Parse(hexStringHash[i].Substring(j * 2, 2), NumberStyles.HexNumber);
            }


            temp.Add(bytes);

        }

        _hashesFromFile = temp;
    }
}


class Program
{

    static void Main()
    {
        Brut brut = new Brut();
        int ans = 0;

        while (ans < 1 || ans > 2)
        {
            try
            {
                Console.Write("1. Read hashes from file.\n2. Read hashes from the console.\n0. Exit\nChoose enter mode: ");
                ans = Int32.Parse(Console.ReadLine());
                switch (ans)
                {
                    case 0:
                        Console.WriteLine("Program shutdown.");
                        return;
                    case 1:
                        Console.Write("Enter the path of file: ");
                        string path = Console.ReadLine();

                        try
                        {
                            brut.ReadHashesFromFile(path);
                        }
                        catch
                        {
                            Console.WriteLine("There is no such file.");
                            Console.WriteLine("Program shutdown.");
                            throw;
                        }
                        break;
                    case 2:
                        brut.ReadHashesFromConsole();
                        break;
                    default:
                        Console.WriteLine("Error. Wrong input. Try again.");
                        break;
                }
            }
            catch (FileNotFoundException e)
            {
                return;
            }
            catch (FormatException e)
            {
                Console.WriteLine("Error. Wrong format. Try again.\n");
            }
        }


        while (true)
        {
            Console.Write("\nEnter the counts of threds (>=1, <=4): ");

            try
            {
                ans = Int32.Parse(Console.ReadLine());
                Console.WriteLine("\nFinding Hashes:");
                brut.Hack(ans, 5);
                break;
            }
            catch (FormatException e)
            {
                Console.WriteLine("Error. Wrong format. Try again.\n");
            }
            catch (Exception e)
            {
                Console.WriteLine("Try again");
            }
        }

        Console.WriteLine("\nEnd of program");
    }
}