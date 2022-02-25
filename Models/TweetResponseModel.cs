namespace TweetNotifier.Models
{

    public class TweetResponseModel
    {
        public Datum[] data { get; set; }
        public Meta meta { get; set; }
    }

    public class Meta
    {
        public string oldest_id { get; set; }
        public string newest_id { get; set; }
        public int result_count { get; set; }
        public string next_token { get; set; }
    }

    public class Datum
    {
        public string id { get; set; }
        public string text { get; set; }
    }

}
