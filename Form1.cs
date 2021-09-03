using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
    using System.Runtime.InteropServices;

namespace TextFinder
{


   
    public partial class Form1 : Form
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();


        public Form1()
        {

            InitializeComponent();
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
            Base mainBase = new Base();

            mainBase.AddDocument(new Document("here is some lorem ipsum text", 1));
            mainBase.AddDocument(new Document("here is some lorem ipsum text but it is much longer",2));
            mainBase.AddDocument(new Document("here is some banana text but it is much longer", 3));
            mainBase.AddDocument(new Document("here is some banana banana banana text but it is much longer", 3));

            Search search = new Search();

            search.GetSearchResult("lorem ipsum", mainBase);
            Console.WriteLine();
            search.GetSearchResult("banana", mainBase);
        }
    }

    public class Base
    {
        public List<Document> documents = new List<Document>();

        public void AddDocument(Document d)
        {
            documents.Add(d);
        }

        public bool RemoveDocument(Document d)
        {
            if (documents.Contains(d))
            {
                documents.Remove(d);
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
            Dictionary<string, double> weights = new Dictionary<string, double>();

            foreach (var w in document.GetWordCount().Keys)
            {
                weights.Add(w, WeightOfWord(w,document));
            }

            document.documentWeights = weights;

            return weights;
        }
    }

    public class Document
    {
        public Document(string title, string text, string date, string time, int documentID)
        {
            this.title = title;
            this.text = text;
            this.date = date;
            this.time = time;
            this.documentID = documentID;
        }

        public Document(string text,int documentID)
        {
            //this.title = title;
            this.text = text;
           // this.date = date;
           // this.time = time;
            this.documentID = documentID;
        }

        public string title;
        public string text;
        public string date;
        public string time;
        public int documentID;

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
            this.documentId = documentId;
            this.title = title;
            this.snippet = snippet;
            this.rank = rank;
            this.date = date;
        }

        public SearchResult(Document document, string snippet, double rank, string date)
        {
            this.documentId = document.documentID;
            this.title = document.title;

            this.snippet = snippet;
            this.rank = rank;
            this.date = date;
        }

        public int documentId;
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
            string[] qWords = GetSearchQueryVector(query);

            //Dictionary<SearchResult,double> results = new Dictionary<SearchResult,double>();

            List<SearchResult> results = new List<SearchResult>();

            double queryEuclideanNorm = Math.Sqrt(qWords.Length);

            foreach (var p in data.documents)
            {
                double score = (ScalarProduct(data.GetDocumentWeights(p), qWords));
                results.Add(new SearchResult(p, "", score, "now"));

                Console.WriteLine($"{score} ::" + p.documentID);
            }

            return results.OrderBy(result => result.rank);
        }
    }
}