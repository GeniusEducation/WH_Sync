using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Dapper;

namespace WH_Sync
{
    class DBConnection
    {

        static string connString;

        static DBConnection()
        {
            connString = "Data Source = 76.160.172.236; Initial Catalog = DDCData_BT; User ID = ddcuser; Password = ddc3214; MultipleActiveResultSets = true";// ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
        }

      

        public static void InsertAmfMapping(string host_amfid, string userid, int roleid)
        {
            string sql = string.Format("insert Client_WH_UserMapping (host_amfid, userid, roleid, dateadded) select '{0}','{1}',{2},getdate()", host_amfid, userid, roleid);
            SqlConnection conn = new SqlConnection(connString);
                       
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public static void InsertWHUserInfo(string userid, string addressid, string phoneid, string email, string bus_email)
        {
            if (string.IsNullOrWhiteSpace(email))
                email = "NULL";
            else
                email = "'" + email + "'";


            if (string.IsNullOrWhiteSpace(bus_email))
                bus_email = "NULL";
            else
                bus_email = "'" + bus_email + "'";


            if (string.IsNullOrWhiteSpace(addressid)) addressid = "NULL";
            if (string.IsNullOrWhiteSpace(phoneid)) phoneid = "NULL";
           
            


            string sql = string.Format("insert Client_WH_UserInfo (userid, home_addressid, home_phoneid, email, bus_email) values ({0},{1},{2},{3},{4})", userid, addressid, phoneid, email, bus_email);
            SqlConnection conn = new SqlConnection(connString);

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public static DataTable GetUserContactInfo()
        {
            string sql = string.Format("exec Client_WH_Data");
            SqlConnection conn = new SqlConnection(connString);
            DataTable dt = new DataTable();

            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                conn.Open();
                da.Fill(dt);
                conn.Close();
            }

            return dt;
        }

        public static void InsertAddress(string userid, string addressid)
        {
            string sql = string.Format("if not exists (select 1 from Client_WH_UserInfo where userid={0}) insert Client_WH_UserInfo (userid, home_addressid) values ({0},{1}) else update Client_WH_UserInfo set home_addressid={1} where userid={0} and home_addressid is null",  userid, addressid);
            SqlConnection conn = new SqlConnection(connString);

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public static void InsertPhone(string userid, string phoneid)
        {
            string sql = string.Format("if not exists (select 1 from Client_WH_UserInfo where userid={0}) insert Client_WH_UserInfo (userid, home_phoneid) values ({0},{1}) else update Client_WH_UserInfo set home_phoneid={1} where userid={0} and home_phoneid is null", userid, phoneid);
            SqlConnection conn = new SqlConnection(connString);

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }



        public static bool UserExists(string userid, int roleid)
        {
            object res;
            bool result = false;
            string sql = string.Format("select 1 from Client_WH_UserMapping where userid='{0}' and roleid={1}", userid, roleid);
            SqlConnection conn = new SqlConnection(connString);

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                res = cmd.ExecuteScalar();
                result = res != null && res.ToString() == "1";
                conn.Close();
            }
            return result;
        }

        public static bool UserInfoExists(string userid)
        {
            object res;
            bool result = false;
            string sql = string.Format("select 1 from Client_WH_UserInfo where userid='{0}'", userid);
            SqlConnection conn = new SqlConnection(connString);

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                res = cmd.ExecuteScalar();
                result = res != null && res.ToString() == "1";
                conn.Close();
            }
            return result;
        }



    }
}

