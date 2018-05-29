using System;
using System.Collections.Generic;
using System.Net;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace WH_Sync
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        static string token;




        static void Main(string[] args)
        {

            Run_WH_Sync().Wait();

            /*string endpoint = "https://bethtfiloh.myschoolapp.com/api/authentication/login/?username=edukelsky&password=ddc3214 ";
           
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetAsync(endpoint).Result;

                return JsonConvert.DeserializeObject<t>(response.Result.Content.ReadAsStringAsync().Result);
            }*/

            //Console.ReadLine();
        }

        //private async Task Run_WH_Sync()
        static private async Task Run_WH_Sync()
        {
            await GetToken();
            //await CreateUserMapping();
            await LoadContactInfo();
        }

        static private async Task GetToken()
        {
            string endpoint = "https://bethtfiloh.myschoolapp.com/api/authentication/login/?username=edukelsky&password=ddc3214 ";

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var stringTask = client.GetStringAsync(endpoint);


            var json = await stringTask;
            var resource = JObject.Parse(json.ToString());
            foreach (var property in resource.Properties())
            {
                if (property.Name == "Token")
                {
                    token = property.Value.ToString();
                }
                //Console.WriteLine("{0} - {1}", property.Name, property.Value);
            }
        }

        static private async Task LoadContactInfo()
        {
            DataTable dt = DBConnection.GetUserContactInfo();

            int addid, phoneid;

            foreach (DataRow row in dt.Rows)
            {
                addid = int.Parse(row["addid"].ToString());
                phoneid = int.Parse(row["phoneid"].ToString());

                if (!String.IsNullOrWhiteSpace(row["Address"].ToString()))
                {
                    if (addid > 0)
                        await UpdateContactInfo(false, row["userid"].ToString(), addid.ToString(), row["HomePhone"].ToString(), row["Address"].ToString(), row["City"].ToString(), row["State"].ToString(), row["Country"].ToString(), row["Zip"].ToString());
                    else
                        await LoadContactInfo(false, row["userid"].ToString(), row["HomePhone"].ToString(), row["Address"].ToString(), row["City"].ToString(), row["State"].ToString(), row["Country"].ToString(), row["Zip"].ToString());
                }

                if (!String.IsNullOrWhiteSpace(row["HomePhone"].ToString()))
                {
                    if (phoneid > 0)
                        await UpdateContactInfo(true, row["userid"].ToString(), phoneid.ToString(), row["HomePhone"].ToString(), row["Address"].ToString(), row["City"].ToString(), row["State"].ToString(), row["Country"].ToString(), row["Zip"].ToString());
                    else
                        await LoadContactInfo(true, row["userid"].ToString(), row["HomePhone"].ToString(), row["Address"].ToString(), row["City"].ToString(), row["State"].ToString(), row["Country"].ToString(), row["Zip"].ToString());
                }

            }

        }

        static private async Task CreateUserMapping()
        {
            /*
            BT Student 3763
            BT Faculty 3764
            BT Parent 3765
            BT Alumni 3766
            BT Staff 3769
            BT Member 4860

            For each call
            // https ://bethtfiloh.myschoolapp.com/api/user/all?t={TOKEN}&roleIDs={commad delimited list of ROLEID} 
            */
            string endpointT = "https://bethtfiloh.myschoolapp.com/api/user/all?t={0}&roleIDs={1}&startrow={2}&endrow={3}";
            string endpoint, userid, hostid,addid, phoneid, email, bus_email;

            int lastrow, numBatches, startrow, endrow, totalrows;

            JObject ject, jectExt, jadd, jphone; ;
            JArray jarrAdd, jarrPhone;

            userid = ""; hostid = "";

            //int[] roles = { 3763, 3764, 3765, 3766, 3769, 4860 };
            int[] roles = {  3769, 4860 }; //3763, 3764, 3765 };
            foreach (int roleid in roles)
            {
                numBatches = 0;
                lastrow = 1;
                totalrows = 200;
                while (lastrow < totalrows)// == 1 || lastrow == 200)
                {
                    startrow = lastrow + 1;// (lastrow * numBatches) + 1;
                    if (startrow > totalrows)
                        startrow = totalrows - 200;
                    endrow = startrow + 200;
                    endpoint = String.Format(endpointT, token, roleid, startrow, endrow);

                    var stringTask = client.GetStringAsync(endpoint);



                    //var jsonArrayString = await stringTask;
                    //var resource = JObject.Parse(json.ToString());

                    var jsonArrayString = await stringTask;
                    JArray jsonArray = JArray.Parse(jsonArrayString);


                    foreach (var j in jsonArray)
                    {
                        //var resource = JObject.Parse(jsonArray[0].ToString());
                        ject = JObject.Parse(j.ToString());
                        userid = ject.GetValue("UserId").ToString();
                        if (!DBConnection.UserExists(userid, roleid)) //!DBConnection.UserExists(i_usr, roleid))
                        {
                            endpoint = String.Format("https://bethtfiloh.myschoolapp.com/api/user/{0}/?t={1}", userid, token);
                            stringTask = client.GetStringAsync(endpoint);
                            var json = await stringTask;
                            hostid = JObject.Parse(json.ToString()).GetValue("HostId").ToString();

                            if (userid != "" && hostid != "")// && Int32.TryParse(hostid, out h_usr))
                                DBConnection.InsertAmfMapping(hostid, userid, roleid);// h_usr, i_usr, roleid);
                            else
                                userid = "";    //no hostid. just continue
                        }

                        if (!DBConnection.UserInfoExists(userid) && userid!="")
                        {
                            addid = ""; phoneid = ""; email = "";

                            endpoint = String.Format("https://bethtfiloh.myschoolapp.com/api/user/extended/all/{0}/?t={1}", userid, token);
                            stringTask = client.GetStringAsync(endpoint);
                            var json = await stringTask;
     
                            jectExt = JObject.Parse(json.ToString());
                            jarrAdd = JArray.Parse(jectExt.GetValue("AddressList").ToString());
                            jarrPhone = JArray.Parse(jectExt.GetValue("PhoneList").ToString());

                            for (int i = 0; i < jarrAdd.Count && addid==""; i++)
                            {
                                jadd = JObject.Parse(jarrAdd[i].ToString());
                                if (jadd.GetValue("address_type").ToString() == "Home")
                                    addid = jadd.GetValue("AddressId").ToString();
                            }

                            for (int i = 0; i < jarrPhone.Count && phoneid==""; i++)
                            {
                                jphone = JObject.Parse(jarrPhone[i].ToString());
                                if (jphone.GetValue("Type").ToString() == "Home")
                                    phoneid = jphone.GetValue("PhoneId").ToString();
                            }
                           
                            email = jectExt.GetValue("Email").ToString();
                            bus_email = jectExt.GetValue("CcEmail").ToString();

                            if (userid != "" && (addid!="" || phoneid!=""))// && Int32.TryParse(hostid, out h_usr))
                                DBConnection.InsertWHUserInfo(userid, addid, phoneid, email, bus_email);// h_usr, i_usr, roleid);
                        }

                        lastrow = Convert.ToInt32(ject.GetValue("RowNumber"));
                        totalrows = Convert.ToInt32(ject.GetValue("RowTotal"));
                    }
                    numBatches++;
                }



            }

        }


        static private async Task LoadContactInfo(bool isPhone, string userid, string phone, string add1, string city, string state, string country, string zip)
        {

            string url = "https://bethtfiloh.myschoolapp.com/api/user/{CONTACTTYPE}foruser?t={TOKEN}&userID={USERID}";
            url = url.Replace("{TOKEN}", token);
            url = url.Replace("{USERID}", userid);


            //string addData = string.Format("{{\"UserId\":{0},\"AddressLine1\":\"{1}\",\"City\":\"{2}\",\"StateShort\":{3},\"Country\":\"{4}\",\"ZipCode\":{5},\"TypeId\":981}}", userid, add1, city, state, country, zip);
            string addData = $@"{{""UserID"":{userid},""AddressLine1"":""{add1}"",""City"":""{city}"",""StateShort"":""{state}"",""Country"":""{country}"",""ZipCode"":{zip},""TypeId"":981}}";
	    //,""AddressTypeLink"": [{{""TypeId"": 981, ""UserId"": {userid}, ""Type"": ""Home"",""SortOrder"": 0, ""Shared"": false}}]}}";

            string phoneData = string.Format("{{\"UserId\":{0},\"PhoneNumber\":\"{1}\",\"TypeId\":1472}}", userid, phone);

            var addContent = new StringContent(addData, System.Text.Encoding.UTF8, "application/json");
            var phoneContent = new StringContent(phoneData, System.Text.Encoding.UTF8, "application/json");

            //1472 = typeid for phone

            /*var stringContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("TypeId", "981"), //HOME = 981 for BT
                new KeyValuePair<string, string>("AddressLine1", add1),                
                new KeyValuePair<string, string>("City", city),
                new KeyValuePair<string, string>("StateShort", state),
                new KeyValuePair<string, string>("ZipCode", zip),
            });*/


            HttpResponseMessage result;

            if (!isPhone)
                result = await client.PostAsync($"{url}".Replace("{CONTACTTYPE}","address"), addContent);
            else
                result = await client.PostAsync($"{url}".Replace("{CONTACTTYPE}","phone"), phoneContent);

            if (result.IsSuccessStatusCode)//.StatusCode == HttpStatusCode.OK)
            {
                string jnewid = await result.Content.ReadAsStringAsync();
                string newid = JObject.Parse(jnewid.ToString()).GetValue("Message").ToString();

                if (!isPhone)
                    DBConnection.InsertAddress(userid, newid);
                else
                    DBConnection.InsertPhone(userid, newid);
            }
            else
                Console.WriteLine(result.StatusCode + " " + result.ReasonPhrase);


        }


        static private async Task UpdateContactInfo(bool isPhone, string userid, string contactid, string phone, string add1, string city, string state, string country, string zip)
        {

            string url = "https://bethtfiloh.myschoolapp.com/api/user/{CONTACTTYPE}/{ID}?t={TOKEN}";
            url = url.Replace("{TOKEN}", token);
            url = url.Replace("{ID}", contactid);


            //string addData = string.Format("{{\"UserId\":{5},\"AddressLine1\":\"{0}\",\"City\":\"{1}\",\"StateShort\":\"{2}\",\"Country\":\"{3}\",\"ZipCode\":\"{4}\",\"TypeId\":981}}", add1, city, state, country, zip, userid);
            string addData = $@"{{""UserID"":{userid},""AddressLine1"":""{add1}"",""City"":""{city}"",""StateShort"":""{state}"",""Country"":""{country}"",""ZipCode"":{zip},""TypeId"":981
	    ,""AddressTypeLink"": [{{""TypeId"": 981, ""UserId"": {userid}, ""Type"": ""Home"",""SortOrder"": 0, ""Shared"": false}}]}}";

            string phoneData = string.Format("{{\"UserId\":{1},\"PhoneNumber\":\"{0}\",\"TypeId\":1472}}",  phone, userid);

            var addContent = new StringContent(addData, System.Text.Encoding.UTF8, "application/json");
            var phoneContent = new StringContent(phoneData, System.Text.Encoding.UTF8, "application/json");

            //1472 = typeid for phone

            /*var stringContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("TypeId", "981"), //HOME = 981 for BT
                new KeyValuePair<string, string>("AddressLine1", add1),                
                new KeyValuePair<string, string>("City", city),
                new KeyValuePair<string, string>("StateShort", state),
                new KeyValuePair<string, string>("ZipCode", zip),
            });*/


            HttpResponseMessage result;

            if (!isPhone)
                result = await client.PutAsync($"{url}".Replace("{CONTACTTYPE}", "address"), addContent);
            else
                result = await client.PutAsync($"{url}".Replace("{CONTACTTYPE}", "phone"), phoneContent);

            if (result.IsSuccessStatusCode)//.StatusCode == HttpStatusCode.OK)
            {
                /*string newid = await result.Content.ReadAsStringAsync();

                if (!isPhone)
                    DBConnection.InsertAddress(userid, newid);
                else
                    DBConnection.InsertPhone(userid, newid);
                    */
            }
            else
                Console.WriteLine(result.StatusCode + " " + result.ReasonPhrase);


        }


    }


}
