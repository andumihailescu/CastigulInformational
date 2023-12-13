using static System.Windows.Forms.LinkLabel;

namespace CastigulInformational
{
    public partial class Form1 : Form
    {
        private string line;
        private string[] values;
        
        private Dictionary<string, int> attributes = new Dictionary<string, int>();
        private Dictionary<string, int> classes = new Dictionary<string, int>();
        private Dictionary<int, Dictionary<Tuple<string[], List<string[]>>, List<string[]>>> data = new Dictionary<int, Dictionary<Tuple<string[], List<string[]>>, List<string[]>>>();
        private Dictionary<string, int> topics = new Dictionary<string, int>();

        private string outputFilePath = "D:\\School\\Data Mining\\CastigulInformational\\OutputFiles\\output.txt";
        private int numberOfFiles = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void loadFileButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = @"D:\School\Data Mining\CastigulInformational\ReutersDataSet";
            openFileDialog1.ShowDialog();

            string filePath = openFileDialog1.FileName;

            ParseFile(filePath);

            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                writer.WriteLine("Topic:");
                foreach (var entry in topics)
                {
                    writer.WriteLine($"{entry.Key}: {entry.Value}");
                }

                List<string> wordsResult = TakeWords();
                writer.WriteLine("\nCuvinte:");
                foreach (var word in wordsResult)
                {
                    writer.WriteLine(word);
                }

                Dictionary<string, int> counts = numberOfWords();
                writer.WriteLine("\nNumar aparitii cuvinte din toate documentele:");
                foreach (var entry in counts)
                {
                    writer.WriteLine($"{entry.Key}: {entry.Value}");
                }

                Dictionary<int, double> E2 = CalculateE2();
                writer.WriteLine("\nCalcul E2:");
                foreach (var entry in E2)
                {
                    writer.WriteLine($"{entry.Key}: {entry.Value}");
                }

                Dictionary<int, double> E3 = CalculateE3();
                writer.WriteLine("\nCalcul E3:");
                foreach (var entry in E3)
                {
                    writer.WriteLine($"{entry.Key}: {entry.Value}");
                }

                List<double> castigInformational = new List<double>();
                double entropieGenerala = CalculEntropieGenerala();
                foreach (var i in wordsResult)
                {
                    int word = int.Parse(i);
                    double p = entropieGenerala + E2[word] + E3[word];
                    castigInformational.Add(p);
                }

                double prag = 1;
                writer.WriteLine("\nRezultate:");
                foreach (var i in castigInformational)
                {
                    if (i >= prag)
                    {
                        writer.WriteLine(i);
                    }
                }
            }
        }

        private void ParseFile(string path)
        {
            using (StreamReader fileStream = new StreamReader(path))
            {
                string line;
                while ((line = fileStream.ReadLine()) != null)
                {
                    string[] words = line.Split();
                    try
                    {
                        if (words[0] == "@attribute")
                        {
                            attributes[words[1]] = int.Parse(words[2]);
                        }
                        else if (words[0] == "@topic")
                        {
                            topics[words[1]] = int.Parse(words[2]);
                        }
                        else if (words[0][0] == '#')
                        {
                            classes[words[0][1..]] = int.Parse(words[1]);
                        }
                        else
                        {
                            int hashIndex = line.IndexOf('#');
                            if (hashIndex != -1 && words[0][1..] != null && !classes.ContainsKey(words[0][1..]))
                            {
                                string result = line[hashIndex..];
                                string[] s = result.Split(' ');
                                s = Array.ConvertAll(s, item => item.Replace('\n'.ToString(), string.Empty)).Where(item => !string.IsNullOrEmpty(item) && item != "#").ToArray();
                                string[] numbers = line.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries)[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                                List<string[]> numberPairs = new();
                                foreach (var num in numbers)
                                {
                                    numberPairs.Add(num.Split(':'));
                                }

                                data[data.Count] = new Dictionary<Tuple<string[], List<string[]>>, List<string[]>>()
                                {
                                    { new Tuple<string[], List<string[]>>(s, numberPairs), numberPairs }
                                };
                                numberOfFiles++;
                            }
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
        }

        private List<string> TakeWords()
        {
            List<string> result = new List<string>();
            foreach (var key in data)
            {
                if (key.Value is Dictionary<Tuple<string[], List<string[]>>, List<string[]>>)
                {
                    foreach (var subKey in key.Value)
                    {
                        foreach (var documentData in subKey.Key.Item2)
                        {
                            result.AddRange(documentData);
                        }
                    }
                }
                /*else if (key.Value is List<string[]>)
                {
                    foreach (var documentData in (List<string[]>)key.Value)
                    {
                        result.AddRange(documentData);
                    }
                }
                else
                {
                    // Handle other cases or raise an exception if unexpected type
                }*/
            }
            return result;
        }



        private double CalculEntropieGenerala()
        {
            double entropieGenerala = 0;
            int toateDocumentele = topics.Values.Sum();
            foreach (var entry in topics)
            {
                double p = entry.Value / (double)toateDocumentele;
                entropieGenerala += p * Math.Log2(p);
            }
            return entropieGenerala;
        }

        private Dictionary<string, int> numberOfWords()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (var entry in data)
            {
                foreach (var subEntry in entry.Value)
                {
                    foreach (var item in subEntry.Value)
                    {
                        string itemKey = item[0];
                        if (!counts.ContainsKey(itemKey))
                        {
                            counts[itemKey] = 0;
                        }
                        counts[itemKey]++;
                    }
                }
            }
            return counts;
        }

        private Dictionary<int, double> CalculateE2()
        {
            Dictionary<int, double> E2 = new ();
            Dictionary<string, Dictionary<string, int>> wordApparitionInClass = numberOfWordOccurencesE2();
            Dictionary<string, int> countItem = numberOfWords();

            foreach (var classWord in wordApparitionInClass)
            {
                foreach (var word in classWord.Value)
                {
                    int index = word.Value;
                    int w = int.Parse(word.Key);
                    double p = index / (double)countItem[w.ToString()];
                    double calc = p * Math.Log2(p);
                    if (E2.ContainsKey(w))
                    {
                        E2[w] -= calc;
                    }
                    else
                    {
                        E2[w] = calc;
                    }
                }
            }
            return E2;
        }

        private Dictionary<string, Dictionary<string, int>> numberOfWordOccurencesE2()
        {
            Dictionary<string, Dictionary<string, int>> c = new ();
            foreach (var entry in data)
            {
                foreach (var subEntry in entry.Value)
                {
                    for (int i = 0; i < subEntry.Key.Item1.Length - 1; i++)
                    {
                        if (!c.ContainsKey(subEntry.Key.Item1[i]))
                        {
                            c[subEntry.Key.Item1[i]] = new Dictionary<string, int>();
                        }
                        foreach (var item in subEntry.Value)
                        {
                            string itemKey = item[0];
                            if (!c[subEntry.Key.Item1[i]].ContainsKey(itemKey))
                            {
                                c[subEntry.Key.Item1[i]][itemKey] = 0;
                            }
                            c[subEntry.Key.Item1[i]][itemKey]++;
                        }
                    }
                }
            }
            return c;
        }

        private Dictionary<int, double> CalculateE3()
        {
            Dictionary<int, double> E3 = new ();
            Dictionary<string, Dictionary<string, int>> wordApparitionInClass = numberOfWordOccurencesE2();
            Dictionary<string, int> countItem = numberOfWords();

            foreach (var classWord in wordApparitionInClass)
            {
                foreach (var word in classWord.Value)
                {
                    //int index = word.Value;
                    int w = int.Parse(word.Key);
                    double p = (numberOfFiles - countItem[w.ToString()]) / (double)numberOfFiles;
                    if (p != 0)
                    {
                        double calc = p * Math.Log2(p);
                        if (E3.ContainsKey(w))
                        {
                            E3[w] -= calc;
                        }
                        else
                        {
                            E3[w] = calc;
                        }
                    }
                }
            }
            return E3;
        }
    }
}