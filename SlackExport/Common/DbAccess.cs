using System.Data;
using MySqlConnector;
using NLog;
using SlackExport.Dto;

namespace SlackExport.Common
{
    public class DbAccess
    {
        // ログを出力するロガー定義
        public static Logger logger = LogManager.GetCurrentClassLogger();

        public List<string> SelectRegisteredDiaryTitleList()
        {
            // 実行するSQL
            string sql = "SELECT title AS title FROM diary";
            using (var connection = new MySqlConnection(Environment.GetEnvironmentVariable("MY_SQL_CONNECTION")))
            {
                using (var command = new MySqlCommand(sql, connection))
                {
                    try
                    {
                        // 接続の確立
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = sql;


                        var dataTable = new DataTable();
                        dataTable.Load(command.ExecuteReader());

                        var list = dataTable.AsEnumerable().Select(x => x["title"].ToString()).ToList();

                        return list;

                    }
                    catch (Exception ex)
                    {
                        logger.Error("登録済みタイトル一括取得のselect文発行で例外エラー。 エラー内容：" + ex.Message + " スタックトレース：" + ex.StackTrace);
                    }

                }
            }
            return [];
        }


        public void InsertDiaryList(List<DataDto> dataDtoList)
        {
            int insertNum = dataDtoList.Count;

            if (insertNum == 0)
            {
                return;
            }

            using (var connection = new MySqlConnection(Environment.GetEnvironmentVariable("MY_SQL_CONNECTION")))
            {
                // 接続の確立
                connection.Open();

                var insertSql = " INSERT INTO diary (title, content, post_date) VALUES " +
                    string.Join(",", dataDtoList.Select((_, index) => $"(@title{index + 1}, @content{index + 1}, @post_date{index + 1})"));

                using (var command = new MySqlCommand(insertSql, connection))
                {
                    try
                    {
                        command.Connection = connection;
                        command.CommandText = insertSql;

                        int valRowNum = 1;
                        foreach (var insertDataDto in dataDtoList)
                        {
                            // バインド変数をセット
                            command.Parameters.AddWithValue("@title" + valRowNum, insertDataDto.title);
                            command.Parameters.AddWithValue("@content" + valRowNum, insertDataDto.message);
                            command.Parameters.AddWithValue("@post_date" + valRowNum, insertDataDto.ts);
                            valRowNum++;
                        }
                        var result = command.ExecuteNonQuery();
                        if (result == 0)
                        {
                            logger.Error("insert文でエラー");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("insert文発行で例外エラー。 エラー内容：" + ex.Message + " スタックトレース：" + ex.StackTrace);
                    }
                }

                connection.Close();
            }
        }
    }
}
