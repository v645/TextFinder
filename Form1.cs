using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        private void Form1_Load(object sender, EventArgs e)
        {
            AllocConsole();
            Main.Start();
        }
    }

    public class Main
    {
        public static void Start()
        {
            Console.Clear();
            Base mainBase = new Base();

            Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetTxt("E:\\sampleText1.txt"))));
            Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetOtherText("E:\\sampleTextPDF.pdf"))));
            Console.Clear();
            Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetOtherText("E:\\sampleWord.docx"))));
            Console.WriteLine("add for " + Benchmark.Measure(() => mainBase.AddDocument(Loader.GetOtherText("E:\\excel.xlsx"))));

            mainBase.AddDocument(new Document("single lorem", "here is some lorem ipsum text"));
            mainBase.AddDocument(new Document("long lorem", "here is some lorem ipsum text but it is much longer"));
            mainBase.AddDocument(new Document("single banana", "here is some banana text but it is much longer"));
            mainBase.AddDocument(new Document("many banana", "here is some banana banana banana text but it is much longer"));

            Search search = new Search();

            Benchmark.MeasureSearch(search, mainBase, "lorem ipsum");
            Console.WriteLine();

            Benchmark.MeasureSearch(search, mainBase, "banana");
            Console.WriteLine();

            Benchmark.MeasureSearch(search, mainBase, "метрика");
            Console.WriteLine();

            mainBase.RemoveDocument(mainBase.GetDocumentByID(6));
            mainBase.RemoveDocument(mainBase.GetDocumentByID(5));

            Benchmark.MeasureSearch(search, mainBase, "lorem ipsum");
            Console.WriteLine();

            Benchmark.MeasureSearch(search, mainBase, "banana");
            Console.WriteLine();

            Benchmark.MeasureSearch(search, mainBase, "метрика");
           /* Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine(mainBase.GetDocumentByID(4).text);
            Console.WriteLine();
            Console.WriteLine();

            foreach (var w in mainBase.GetDocumentByID(4).text.Split('\t'))
            {

            Console.WriteLine(w);
            }
            */
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

            SetDocumentRefreshFlag();
        }

        private void SetDocumentRefreshFlag()
        {
            foreach (var d in documents)
            {
                d.needRecalculateWeights = true;
            }
        }

        public bool RemoveDocument(Document d)
        {
            if (documents.Contains(d))
            {
                documents.Remove(d);
                SetDocumentRefreshFlag();
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

            foreach (var d in document.GetWordCount())
            {
                int wordCount = d.Value;
                string wordOther = d.Key;

                double w = wordWeighted(wordOther, wordCount, totalDocuments);

                totalWeightSumSq += w * w;
            }

            return wordWeight / Math.Sqrt(totalWeightSumSq);
        }

        private double wordWeighted(string word, Document document)
        {
            float Ndk = document.GetWordCount(word); //число встреч  k-го слова (признака) в документе  d,
            float Nk = GetDocumentCountWithWord(word); //Nk – число документов,  содержащих k-ое слово (признак),
            float N = documents.Count; //N   - общее число рассматриваемых документов.

            double Wdk = (Ndk * Math.Log(N / Nk));

            return Wdk;
        }

        private double wordWeighted(string word, int wordCount, int documentsCount)
        {
            float Ndk = wordCount; //число встреч  k-го слова (признака) в документе  d,
            float Nk = GetDocumentCountWithWord(word); //Nk – число документов,  содержащих k-ое слово (признак),
            float N = documentsCount; //N   - общее число рассматриваемых документов.

            double Wdk = (Ndk * Math.Log(N / Nk));

            return Wdk;
        }

        public Dictionary<string, double> GetDocumentWeights(Document document)
        {
            if (document.documentWeights.Count != 0 && !document.needRecalculateWeights) { return document.documentWeights; }

            Dictionary<string, double> weights = new Dictionary<string, double>();

            /* Console.WriteLine("GetDocumentWeights for"+
             Benchmark.Measure(() =>
             { */
            foreach (var w in document.GetWordCount().Keys)
            {
                weights.Add(w, WeightOfWord(w, document));
            }
            //}) +"ms");

            document.documentWeights = weights;
            document.needRecalculateWeights = false;

            return weights;
        }
    }

    public class Loader
    {
        public static Document GetTxt(string path)
        {
            if (!File.Exists(path)) { PrintLoadError("No file [" + path + "]"); return Document.Empty(); }

            string name = Path.GetFileName(path);
            DateTime creationTime = File.GetCreationTime(path);

            Document newDocument = new Document(name, creationTime);

            StreamReader stream = new StreamReader(path);
            string content = stream.ReadToEnd();
            stream.Close();

            newDocument.text = content;

            // var text = new TextExtractor().Extract(path).Text;

            return newDocument;
        }

        public static Document GetOtherText(string path)
        {
            if (!File.Exists(path)) { PrintLoadError("No file [" + path + "]"); return Document.Empty(); }

            string name = Path.GetFileName(path);
            string format = Path.GetExtension(path);
            DateTime creationTime = File.GetCreationTime(path);

            Document newDocument = new Document(name, creationTime);

            var content = new TextExtractor().Extract(path).Text.Trim();

            switch (format)
            {
                case ".xlsx":
                    string plainText = "";
                    string[] rows = content.Split('\t');
                    foreach (var r in rows)
                    {
                        plainText += r.Trim()+" ";
                    }
                    content = plainText;
                    break;
                default:  break;
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

        public Document(string title, DateTime creationTime)
        {
            this.title = title;
            this.date = creationTime.Date.ToShortDateString();
            this.time = creationTime.Date.ToShortTimeString();
        }

        public Document(string text)
        {
            this.text = text;
        }

        public Document(string title,string text)
        {
            this.text = text;
            this.title = title;
        }

        public string title;
        public string text;
        public string date;
        public string time;
        public int documentID;

        public bool needRecalculateWeights = false;

        public Dictionary<string, double> documentWeights = new Dictionary<string, double>();

        private Dictionary<string, int> wordCount = new Dictionary<string, int>();

        public Dictionary<string, int> GetWordCount()
        {
            if (wordCount.Count == 0)
            {
                wordCount = CalculateWordCount();
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
               // string trimmedWord = w.Trim()
                if (!wordCount.ContainsKey(w))
                {
                    wordCount.Add(w, 1);
                }
                else
                {
                    wordCount[w]++;
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
                    results.Add(new SearchResult(p, "", score, "now"));
                }
            }

            var orderedResult = results.OrderBy(result => result.rank);
            foreach (var p in orderedResult)
            {
                Console.WriteLine($" {p.rank}=={p.title} [id={p.documentID}]");
            }

            return orderedResult;
        }
    }
}