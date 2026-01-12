namespace RAG.Models;
    public class Item
    {
        public string ?Title { get; set; }
        public string ?Content { get; set; }
        public int Score { get; set; }
        public float[]? Embedding { get; set; }
    }

