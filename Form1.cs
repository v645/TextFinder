using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using TikaOnDotNet.TextExtraction;

namespace TextFinder
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        public Form1()
        {
            InitializeComponent();

            // OpenNLP.Tools.Parser.Parse parse = new OpenNLP.Tools.Parser.Parse("kitten",)
        }

        private Base mainBase;

        private void Form1_Load(object sender, EventArgs e)
        {
            AllocConsole();
            

            label1.Text = trackBar1.Value + "";
            Main.accuracy = trackBar1.Value;

            label2.Text = trackBar2.Value + "";
            Main.minWordLength = trackBar2.Value;

            label3.Text = trackBar3.Value + "";
            Main.minimalWordUsingsCount = trackBar3.Value;


            mainBase = Main.StartBase();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Search search = new Search();
            Benchmark.MeasureSearch(search, mainBase, textBox1.Text);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label1.Text = trackBar1.Value + "";
            Main.accuracy = trackBar1.Value;
            mainBase.SetDocumentRefreshCountFlag();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            label2.Text = trackBar2.Value + "";
            Main.minWordLength = trackBar2.Value;
            mainBase.SetDocumentRefreshCountFlag();
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            label3.Text = trackBar3.Value + "";
            Main.minimalWordUsingsCount = trackBar3.Value;
            mainBase.SetDocumentRefreshCountFlag();
        }

        private void label3_Click(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Console.WriteLine($"size = {sizeof( mainBase.documents)}")
            foreach (var d in mainBase.documents)
            {
                d.text = "";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            mainBase.ForceCacheDocumentWeights();
        }
    }

    public class Main
    {
        public static float accuracy = 100;
        public static int minWordLength = 2;
        public static int minimalWordUsingsCount = 2;

        public static bool createIndexOnLoad = true;

        public static Base StartBase()
        {
            //Console.Clear();
            Loader.GetExtractor();//.Extract();
            Base mainBase = new Base();

            Console.WriteLine("add folder for " + Benchmark.Measure(() => Loader.AddFolder("E:\\folder", mainBase)) + "ms");

            //Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetTxt("E:\\folder\\МитиоКакуГиперпространство.txt"))));
            /*
            Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetTxt("E:\\sampleText1.txt"))));
            Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetOtherText("E:\\sampleTextPDF.pdf"))));
            Console.Clear();
            Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetOtherText("E:\\sampleWord.docx"))));
            Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetOtherText("E:\\excel.xlsx"))));*/

            /* mainBase.AddDocument(new Document("single lorem", "here is some lorem ipsum text"));
             mainBase.AddDocument(new Document("long lorem", "here is some lorem ipsum text but it is much longer"));
             mainBase.AddDocument(new Document("single banana", "here is some banana text but it is much longer"));
             mainBase.AddDocument(new Document("many banana", "here is some banana banana banana text but it is much longer"));*/

           // mainBase.ForceCacheDocumentWeights();

            return mainBase;
        }
    }

    public class Benchmark
    {
        public static long Measure(Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            action.Invoke();

            stopwatch.Stop();
            var result = stopwatch.ElapsedMilliseconds;

            return result;
        }

        public static void MeasureAdding(Base mainBase, Document newDocument)
        {
            Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetTxt("E:\\sampleText1.txt"))));
        }

        public static void MeasureSearch(Search search, Base mainBase, string searchText)
        {
            Console.WriteLine("search for " + Benchmark.Measure(() => search.GetSearchResult(searchText, mainBase)) + "ms");
        }
    }

    public class Base
    {
        public int lastID = 0;
        public List<Document> documents = new List<Document>();

        public void AddDocument(Document d)
        {
            documents.Add(d);
            d.documentID = ++lastID;

            SetDocumentRefreshWeightFlag();
        }

        private void SetDocumentRefreshWeightFlag()
        {
            foreach (var d in documents)
            {
                d.needRecalculateWeights = true;
            }
        }

        public void SetDocumentRefreshCountFlag()
        {
            foreach (var d in documents)
            {
                d.needRecalculateCount = true;
            }
            SetDocumentRefreshWeightFlag();
        }

        public bool RemoveDocument(Document d)
        {
            if (documents.Contains(d))
            {
                documents.Remove(d);
                SetDocumentRefreshWeightFlag();
                return true;
            }

            return false;
        }

        public Document GetDocumentByID(int documentID)
        {
            foreach (var d in documents)
            {
                if (d.documentID == documentID)
                {
                    return d;
                }
            }

            return null;
        }

        public int GetDocumentCountWithWord(string word)
        {
            int documentCount = 0;
            foreach (var d in documents)
            {
                if (d.GetWordCount().ContainsKey(word)) documentCount++;
            }

            return documentCount;
        }

        public double WeightOfWord(string word, Document document)
        {
            double wordWeight = wordWeighted(word, document);

            int totalDocuments = documents.Count;
            double totalWeightSumSq = 0;

            Parallel.ForEach(document.GetWordCount(), (d) =>
             {
                 int wordCount = d.Value;
                 string wordOther = d.Key;

                 double w = wordWeighted(wordOther, wordCount, totalDocuments);

                 totalWeightSumSq += w * w;
             });
           /* foreach (var d in document.GetWordCount())
             {
                 int wordCount = d.Value;
                 string wordOther = d.Key;

                 double w = wordWeighted(wordOther, wordCount, totalDocuments);

                 totalWeightSumSq += w * w;
             }*/

            return wordWeight / Math.Sqrt(totalWeightSumSq);
        }

        private double wordWeighted(string word, Document document)
        {
            return wordWeighted(word, document.GetWordCount(word), documents.Count);
        }

        private double wordWeighted(string word, int wordCount, int documentsCount)
        {
            float Ndk = wordCount; //число встреч  k-го слова (признака) в документе  d,
            float Nk = GetDocumentCountWithWord(word); //Nk – число документов,  содержащих k-ое слово (признак),
            float N = documentsCount; //N   - общее число рассматриваемых документов.

            double Wdk = (Ndk * Math.Log(N / Nk));

            return Wdk;
        }

        public void ForceCacheDocumentWeights()
        {
            Console.WriteLine("GetDocumentWeights(caching) for" +
            Benchmark.Measure(() =>
            {
                Parallel.ForEach(documents, (document) => GetDocumentWeights(document));

               /* foreach (var document in documents)
                {
                    
                }*/
                /*foreach (var document in documents)
                {
                    GetDocumentWeights(document);
                }*/
            }) + "ms");
        }

        public Dictionary<string, double> GetDocumentWeights(Document document)
        {
            if (document.documentWeights.Count != 0 && !document.needRecalculateWeights) { return document.documentWeights; }

            Dictionary<string, double> weights = new Dictionary<string, double>();

            Console.WriteLine("GetDocumentWeights for" +
            Benchmark.Measure(() =>
            {
                /* List<Task> tasks = new List<Task>();
                 foreach (var w in document.GetWordCount().Keys)
                 {
                    tasks.Add( Task.Run(()=> weights.Add(w, WeightOfWord(w, document))));
                 }

                 Task.WaitAll(tasks.ToArray());*/

                foreach (var w in document.GetWordCount().Keys)
                {
                    double weight = WeightOfWord(w, document);

                    weights.Add(w, weight);
                }

                /* foreach (var w in document.GetWordCount().Keys)
                 {
                     weights.Add(w, WeightOfWord(w, document));
                 }*/
            }) + "ms");

            document.documentWeights = weights;
            document.needRecalculateWeights = false;

            return weights;
        }
    }

    public class Loader
    {
        public static List<Document> AddFolder(string path, Base mainBase)
        {
            if (!Directory.Exists(path)) { PrintLoadError("No directory [" + path + "]"); return new List<Document>(); }

            List<Document> files = new List<Document>();

            List<Task> tasks = new List<Task>();

            foreach (var p in Directory.GetFiles(path))
            {
                Console.WriteLine(p);

                Task t = Task.Run(() =>
               {
                   Console.WriteLine("add file for " + Benchmark.Measure(() =>
                   {
                       Document doc = GetOtherText(p);

                       if (doc.text != "###" && doc.text != "")
                       {
                           files.Add(doc);
                           
                       }
                   }
                   )
                       );
               });
                tasks.Add(t);
            }

           

            Task.WaitAll(tasks.ToArray());

            if (Main.createIndexOnLoad)
            {
                
                Console.WriteLine("add snippets for " + Benchmark.Measure(() =>
                {
                    //tasks.Clear();
                    foreach (var doc in files)
                    {
                       var t = Task.Run(() => { doc.GetWordCount(); doc.GetSnippets(); });
                        //t.Start();
                       // tasks.Add(t);


                    }
                    //Task.WaitAll(tasks.ToArray());
                }));
            }

            foreach (var f in files)
            {
                mainBase.AddDocument(f);
            }

            return files;
        }

        public static Document GetTxt(string path)
        {
            if (!File.Exists(path)) { PrintLoadError("No file [" + path + "]"); return Document.Empty(); }

            string name = Path.GetFileName(path);
            DateTime creationTime = File.GetCreationTime(path);

            Document newDocument = new Document(name, creationTime,path);

            StreamReader stream = new StreamReader(path);
            string content = stream.ReadToEnd();
            stream.Close();

            newDocument.text = content;

            // var text = new TextExtractor().Extract(path).Text;

            return newDocument;
        }

        private static TextExtractor textExtractor;

        public static TextExtractor GetExtractor()
        {
            if (textExtractor == null)
            {
                textExtractor = new TextExtractor();

                string path = Directory.GetCurrentDirectory() + "\\p.txt";

                //File.Create(path);

                StreamWriter stream = new StreamWriter(path);
                stream.WriteLine("its ok!");
                stream.Flush();
                stream.Close();
                textExtractor.Extract(path);
                //Console.Clear();
            }

            return textExtractor;
        }

        public static Document GetOtherText(string path)
        {
            if (!File.Exists(path)) { PrintLoadError("No file [" + path + "]"); return Document.Empty(); }

            string name = Path.GetFileName(path);
            string format = Path.GetExtension(path);
            DateTime creationTime = File.GetCreationTime(path);

            Document newDocument = new Document(name, creationTime,path);

            var content = GetExtractor().Extract(path).Text.Trim();

            switch (format)
            {
                case ".xlsx":
                    string plainText = "";
                    string[] rows = content.Split('\t');
                    foreach (var r in rows)
                    {
                        plainText += r.Trim() + " ";
                    }
                    content = plainText;
                    break;

                default: break;
            }

            newDocument.text = content;

            return newDocument;
        }

        public static void PrintLoadError(string error)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;

            Console.WriteLine(error);

            Console.BackgroundColor = ConsoleColor.Black;
        }
    }

    public class Document
    {
        public static Document Empty()
        {
            return new Document("###");
        }

        public Document(string title, DateTime creationTime,string path)
        {
            this.title = title;
            this.filePath = path;
            this.date = creationTime.Date.ToShortDateString();
            this.time = creationTime.Date.ToShortTimeString();
        }

        public Document(string text)
        {
            this.text = text;
        }

        public Document(string title, string text)
        {
            this.text = text;
            this.title = title;
        }

        public string filePath;
        public string title;
        public string text;
        public string date;
        public string time;
        public int documentID;

        public bool needRecalculateWeights = false;
        public bool needRecalculateCount = false;

        public Dictionary<string, double> documentWeights = new Dictionary<string, double>();

        private Dictionary<string, int> wordCount = new Dictionary<string, int>();

        public Dictionary<string, string> snippets = new Dictionary<string, string>();

        public Dictionary<string, string> GetSnippets()
        {
            if (snippets.Count == 0 || needRecalculateCount)
            {
                snippets = CalculateSnippets(GetWordCount());
                //needRecalculateCount = false;
            }

            return snippets;
        }

        Dictionary<string, string> CalculateSnippets(Dictionary<string, int> wordCounts)
        {
            Dictionary<string, string> snippets = new Dictionary<string, string>(wordCount.Count+5);

            Console.WriteLine($"calc {wordCount.Count} snippet for " + Benchmark.Measure(() =>
            {
                foreach (string w in wordCounts.Keys)
                {
                    snippets.Add(w,"");
                }
                List<string> wordKeys = new List<string>(wordCount.Count);
                wordKeys.AddRange(wordCount.Keys);

                Parallel.For(0, wordCounts.Count, (i) => {

                    string w = wordKeys[i];
                    int wordLength = w.Length;
                    int wordPosition = text.IndexOf(w + " ");

                    int startMargin = 10;

                    if (wordPosition <= startMargin)
                    {
                        startMargin = 0;
                    }
                    string snipp = text.Substring(wordPosition - startMargin, wordLength + 25);
                    snippets[w] = snipp;

                });
                /*
                foreach (string w in wordCounts.Keys)
                {
                    int wordLength = w.Length;
                    int wordPosition = text.IndexOf(w+" ");

                    int startMargin = 10;

                    if (wordPosition <= startMargin)
                    {
                        startMargin = 0;
                    }
                    string snipp = text.Substring(wordPosition - startMargin, wordLength + 25);
                    snippets.Add(w, snipp);
                }*/
            }));

            return snippets;
        }

        public Dictionary<string, int> GetWordCount()
        {
            if (wordCount.Count == 0 || needRecalculateCount)
            {
                wordCount = CalculateWordCount();
                needRecalculateCount = false;
            }

            return wordCount;
        }

        public int GetWordCount(string word)
        {
            return GetWordCount()[word];
        }

        private Dictionary<string, int> CalculateWordCount()
        {
            string[] words = text.Split(' ');

            foreach (var w in words)
            {
                string trimmedWord = w.Trim().ToLower();
                if (!wordCount.ContainsKey(w))
                {
                    wordCount.Add(w, 1);
                }
                else
                {
                    wordCount[w]++;
                }
            }

            bool useAll = false;

            if (!useAll)
            {
                int minimalWordGate = 50;
                int keywords = (int)Math.Pow(wordCount.Count, (Main.accuracy / 100f));

                int minimalWordUsingsCount = Main.minimalWordUsingsCount; //(int)Math.Pow(wordCount.Count, 1f / 5f);

                if (wordCount.Count > minimalWordGate)
                {
                    int startSize = wordCount.Count;
                    int removed = 0;

                    long ellapsedMs =
                    Benchmark.Measure(() =>
                      {
                          Dictionary<string, int> newWordCount = new Dictionary<string, int>(wordCount);

                          foreach (var w in wordCount)
                          {
                              if (w.Value < minimalWordUsingsCount)
                              {
                                  newWordCount.Remove(w.Key);
                                  removed++;
                              }
                              else if (w.Key.Length < Main.minWordLength)
                              {
                                  newWordCount.Remove(w.Key);
                                  removed++;
                              }
                          }
                          if (newWordCount.Count > 0)
                          {
                              wordCount = newWordCount.OrderByDescending(x => x.Value).Take(keywords).ToDictionary(p => p.Key, p => p.Value);
                          }//Console.WriteLine("removed " + removed);
                      });

                    Console.WriteLine($"get top {keywords} words form {startSize}->{wordCount.Count} [gate={minimalWordUsingsCount}] in {ellapsedMs}ms");
                }
            }

            return wordCount;
        }
    }

    public class SearchResult
    {
        public SearchResult(int documentId, string title, string snippet, double rank, string date)
        {
            this.documentID = documentId;
            this.title = title;
            this.snippet = snippet;
            this.rank = rank;
            this.date = date;
        }

        public SearchResult(Document document, string snippet, double rank, string date)
        {
            this.documentID = document.documentID;
            this.title = document.title;

            this.snippet = snippet;
            this.rank = rank;
            this.date = date;
        }

        public int documentID;
        public string title;
        public string snippet;
        public double rank;
        public string date;
    }

    public class Search
    {
        public bool allWordsTogether;

        public string dateStartString;

        public string dateEndString;
        public string searchQuery;
        /*
                private Dictionary<string, double> GetSearchQueryVector(string query, Dictionary<string, double> docVector)
                {
                    string[] queryWords = query.Split(' ');
                    //todo: each to nltk

                    return
                }
                */

        private string[] GetSearchQueryVector(string query)
        {
            query = query.ToLower();
            string[] queryWords = query.Split(' ');

            //todo: each to nltk

            return queryWords;
        }

        private double ScalarProduct(Dictionary<string, double> allWordsWeights, string[] queryWords)
        {
            double result = 0;

            foreach (var searchedWord in queryWords)
            {
                double searchedWordWeight = 0;
                bool isInDict = allWordsWeights.TryGetValue(searchedWord, out searchedWordWeight);
                if (isInDict)
                {
                    result += searchedWordWeight;
                }
            }

            return result;
        }

        private double EuclideanNorm(Dictionary<string, double> a)
        {
            double sqSum = 0;
            foreach (var w in a)
            {
                sqSum += Math.Abs(w.Value);
            }

            return Math.Sqrt(sqSum);
        }

        public string GetSnippets(Document d, string[] query)
        {
            string s = "";

            foreach (var w in query)
            {
                string snippet = "";
                d.GetSnippets().TryGetValue(w, out snippet);

                s += snippet + "\n";
            }
            return s;
        }

        public IOrderedEnumerable<SearchResult> GetSearchResult(string query, Base data)
        {
            Console.WriteLine($"search [{query}] in {data.documents.Count} documents");

            string[] qWords = GetSearchQueryVector(query);
            //Dictionary<SearchResult,double> results = new Dictionary<SearchResult,double>();

            List<SearchResult> results = new List<SearchResult>();

            double queryEuclideanNorm = Math.Sqrt(qWords.Length);

            
            foreach (var p in data.documents)
            {
                double score = (ScalarProduct(data.GetDocumentWeights(p), qWords));

                if (score > 0)
                {
                    results.Add(new SearchResult(p, GetSnippets(p,qWords), score, "nosw"));
                }
            }
            
            var orderedResult = results. OrderByDescending(result => result.rank);
            foreach (var p in orderedResult)
            {
                Console.WriteLine($" {p.rank:0.000} = {p.title} [id={p.documentID}] [...{p.snippet}...]");
            }

            return orderedResult;
        }
    }
}