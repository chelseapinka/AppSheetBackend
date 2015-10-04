using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using PhoneNumbers;

namespace AppSheet
{  
    #region Data contracts
    [DataContract]
    public class User
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "age")]
        public int Age { get; set; }
        [DataMember(Name = "number")]
        public string Phone { get; set; }
    }

    [DataContract]
    public class UserIdList
    {
        [DataMember(Name = "result")]
        public List<int> Result { get; set; }
        [DataMember(Name = "token")]
        public string Token { get; set; }
    }
    #endregion

    class AppSheet
    {
        /// <summary>
        /// The valid us phone number length
        /// </summary>
        private const int US_NUMBER_LENGTH = 10;

        /// <summary>
        /// The base URL.
        /// </summary>
        private readonly string baseUrl;

        /// <summary>
        /// The list of users with valid phone numbers.
        /// </summary>
        private List<User> validUsers;

        public AppSheet()
        {
            validUsers = new List<User>();
            baseUrl = ConfigurationSettings.AppSettings["BaseUrl"];
        }

        /// <summary>
        /// Gets the list of user identifiers.
        /// </summary>
        /// <returns>The list of user identifiers, with token if applicable.</returns>
        /// <param name="token"> token to load next page of users</param>
        private UserIdList GetListOfUserIds(string token)
        {
            var listUrl = string.Format("{0}/list", baseUrl);
           
            if (!String.IsNullOrEmpty(token))
            {
                listUrl += string.Format("?token={0}", token);
            }
            return GetResource(listUrl, typeof(UserIdList)) as UserIdList;
        }

        /// <summary>
        /// Loads the valid users.
        /// </summary>
        /// <param name="userIdList">User identifier list.</param>
        private void LoadValidUsers(UserIdList userIdList)
        {
            List<int> userIds = userIdList.Result;
            foreach (var userId in userIds) 
            {
                User user = GetUserDetails(userId);
                if (IsValidPhoneNumber(user.Phone))
                {
                    validUsers.Add(user); // Adding only users with valid phone numbers
                }
            }
        }

        /// <summary>
        /// Gets the user details.
        /// </summary>
        /// <returns>User object from parsed JSON</returns>
        /// <param name="userId">User identifier.</param>
        private User GetUserDetails(int userId)
        {
            return GetResource(string.Format("{0}/detail/{1}", baseUrl, userId), typeof(User)) as User;
        }

        /// <summary>
        /// Gets required response from endpoint service.
        /// </summary>
        /// <returns> Object created from request data.</returns>
        /// <param name="url">URL.</param>
        /// <param name="type">Type.</param>
        private object GetResource(string url, Type type)
        {
            var request = WebRequest.Create(url) as HttpWebRequest;
            using (var response = request.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("Server error (HTTP {0}: {1}).",
                                                      response.StatusCode,
                                                      response.StatusDescription));
                }
                var jsonSerializer = new DataContractJsonSerializer(type);
                var objResponse = jsonSerializer.ReadObject(response.GetResponseStream());
                return objResponse;
            }
        }

        /// <summary>
        /// Determines whether this is a valid phone number.
        /// </summary>
        /// <returns><c>true</c> if this instance is valid phone number ; otherwise, <c>false</c>.</returns>
        /// <param name="phone">Phone.</param>
        private bool IsValidPhoneNumber(string phone)
        {
            PhoneNumber number;
            var instance = PhoneNumberUtil.GetInstance();
            try
            {
                number = instance.Parse(phone, "US");
            }
            catch (NumberParseException)
            {
                return false;
            }
            // To distinguish between valid numbers without an area code
            var lengthIsValid = number.NationalNumber.ToString().Length == US_NUMBER_LENGTH;
            return instance.IsPossibleNumber(number) && lengthIsValid;
        }

        /// <summary>
        /// Gets the youngest users from valid users.
        /// </summary>
        /// <returns>Returns num of the youngest users</returns>
        /// <param name="n">Num</param>
        private IEnumerable<User> GetYoungest(int num)
        {
            return validUsers.OrderBy(o => o.Age).Take(num);
        }

        /// <summary>
        /// Runs web requests/response, sorting and printing to screen
        /// </summary>
        public void Run()
        {
            UserIdList list;
            string token = string.Empty;
            do
            {
                list = GetListOfUserIds(token);
                LoadValidUsers(list);
                token = list.Token;
            } while (!string.IsNullOrEmpty(token)); // looping thru pages with the additional tokens

            var topFive = GetYoungest(5).OrderBy(o => o.Name);
            foreach (var person in topFive) {
                Console.WriteLine("{0} has number {1} and is {2} years old", person.Name, person.Phone, person.Age);
            }
        }

       public static void Main(string[] args)
        {
            var appSheet = new AppSheet();
            appSheet.Run();
        }
    }
}
