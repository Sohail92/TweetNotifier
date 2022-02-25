using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using TweetNotifier.Models;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TweetNotifier
{
    public class Function
    {
        // Need to make environment variables
        private const string TwitterUsersApiURL = "https://api.twitter.com/2/users/";
        private const string UserID = "252569527"; //SR-"1484312565291311104"; // MartinLewis-"252569527";
        private const string BearerValue = "{insert bearer token here}";
        private const int EventTriggerRuleInMins = 10;
        private const string SNSTopicARN = "arn:aws:sns:eu-west-2:742261515079:SR-Test";

        public async Task<string> FunctionHandler(ILambdaContext context)
        {
            int numberOfTweetsFound = 0;

            string urlToHit = $"{TwitterUsersApiURL}{UserID}/tweets?exclude=retweets,replies&start_time={DateTime.Now.AddMinutes(-EventTriggerRuleInMins):s}Z";
            LambdaLogger.Log($"Lambda function will make a GET request to: {urlToHit} for tweet information");

            var httpClient = SetupHttpClient();
            var result = await httpClient.GetAsync(urlToHit);

            if (result.IsSuccessStatusCode)
            {
                var responseBody = await result.Content.ReadAsStringAsync();
                LambdaLogger.Log($"ResponseBody: {responseBody}");

                TweetResponseModel twitterResponse = JsonSerializer.Deserialize<TweetResponseModel>(responseBody);
                
                if (twitterResponse != null && twitterResponse.data?.Length > 0)
                {
                    numberOfTweetsFound = twitterResponse.data.Length;
                    foreach (var tweet in twitterResponse.data)
                    {
                        string textToLookOutFor = "insurance";
                        if (tweet.text.Contains(textToLookOutFor))
                        {
                            string userResponse = await httpClient.GetStringAsync($"{TwitterUsersApiURL}{UserID}");
                            var userDetails = JsonSerializer.Deserialize<UserResponseModel>(userResponse);

                            string notificationString = $"Found tweet with text: {tweet.text} from {userDetails.UserData.username} at {DateTime.Now}";

                            LambdaLogger.Log(notificationString);
                            await PushToSNS(notificationString);
                        }
                    }
                }
            }
            else
            {
                LambdaLogger.Log($"Failure response code when attempting to hit {urlToHit} - Result = {result}");
            }

            return $"{numberOfTweetsFound} new tweet{{s}} found";
        }

        private HttpClient SetupHttpClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {BearerValue}");
            return client;
        }

        private async Task PushToSNS(string message)
        {
            try
            {
                var client = new AmazonSimpleNotificationServiceClient(RegionEndpoint.EUWest2);
                var request = new PublishRequest
                {
                    TopicArn = SNSTopicARN,
                    Message = message
                };
                await client.PublishAsync(request);
            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"Error sending {message} to {SNSTopicARN}. Details are: {ex.Message}");
            }

        }
    }
}

//--NOTES
// psuedocode: for a given username, get the user id, make a request to get their last tweet in the last {x} minutes,
// where x is the time for the cloudwatch trigger, if we have no tweets since then, terminate, else if we have tweets, then notify using AWS SNS
//
// Martin Lewis user information: https://api.twitter.com/2/users/by/username/MartinSLewis
// gives us his ID as 252569527
// get tweets: https://api.twitter.com/2/users/:id/tweets
// get tweets for martin lewis: https://api.twitter.com/2/users/252569527/tweets
// get martin lewis tweets from a certain time: e.g. https://api.twitter.com/2/users/252569527/tweets?start_time=2022-02-02T18:00:00Z
//
// How to run a lambda on a schedule: https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/RunLambdaSchedule.html
// API Tools and Libs: https://developer.twitter.com/en/docs/twitter-api/tools-and-libraries/v2
// Command to deploy lambda to a zip file: dotnet lambda package deploy-function.zip
