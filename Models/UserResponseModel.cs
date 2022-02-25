using System.Text.Json.Serialization;

namespace TweetNotifier.Models
{
    public class UserResponseModel
    {
        [JsonPropertyName("data")]
        public Data UserData { get; set; }

        public class Data
        {
            public string id { get; set; }
            public string name { get; set; }
            public string username { get; set; }
        }
    }
}
