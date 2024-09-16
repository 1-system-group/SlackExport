using System.Data;
using MySqlConnector;
using NLog;
using SlackExport.Dto;

namespace SlackExport.Common
{
    public class DbAccess
    {
        // 一度に登録する件数の上限
        private static readonly int BATCH_INSERT_MAX_NUM = 1000;

        // ログを出力するロガー定義
        public static Logger logger = LogManager.GetCurrentClassLogger();

        public bool InsertDiary(DataDto dataDto)
        {
            // 実行するSQL
            var sql = " INSERT INTO diary (title, content, post_date, update_date) VALUES (@title, @content, @post_date, @update_date)";

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
                        // バインド変数をセット
                        // タイトルは「チャンネル名 + 日時 + ユーザ名」の形式にする
                        command.Parameters.AddWithValue("@title", dataDto.title);
                        command.Parameters.AddWithValue("@content", dataDto.message);
                        command.Parameters.AddWithValue("@post_date", dataDto.ts);
                        command.Parameters.AddWithValue("@update_date", dataDto.ts);

                        var result = command.ExecuteNonQuery();
                        if (result != 1)
                        {
                            logger.Error("insert文でエラー。タイトル：" + dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user);
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        logger.Error("insert文発行で例外エラー。タイトル：" + dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user + " エラー内容：" + ex.Message + " スタックトレース：" + ex.StackTrace);
                    }
                }
            }
            return true;
        }

        public bool CheckRegisteredDiary(DataDto dataDto)
        {
            // 実行するSQL
            string sql = "SELECT COUNT(*) AS count FROM diary WHERE title = @title";
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

                        // タイトル
                        string title = dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user;

                        // バインド変数をセット
                        command.Parameters.AddWithValue("@title", title);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {

                                if (int.Parse(reader["count"].ToString()) > 0)
                                {
                                    logger.Info("登録済み：" + title);
                                    return true;
                                }
                            }
                            else
                            {
                                logger.Error("select文でエラー。：" + dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user);
                                return true;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        logger.Error("select文発行で例外エラー。タイトル：" + dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user + " エラー内容：" + ex.Message + " スタックトレース：" + ex.StackTrace);
                    }

                    return false;
                }
            }
        }

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


        public void InsertsDiary(List<DataDto> dataDtoList)
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

                // 一括登録件数単位のカウンタ
                int batchInsertNum = (insertNum / BATCH_INSERT_MAX_NUM) + 1;

                for (int i = 0; i < batchInsertNum; i++)
                {
                    // 上限件数毎に抜き出して登録していく
                    // 仮に上限件数が1000だとしたら、
                    // start:0 end:1000、start:1000 end:1000、start:2000 end:1000
                    // というように1000件ずつ抜き出していく
                    int startNum = BATCH_INSERT_MAX_NUM * i;
                    int endNum = BATCH_INSERT_MAX_NUM;
                    // endが上限未満の場合は、あるところまでに補正する
                    // 仮に上限件数が1000件の場合に、リストが600件まで場合は、satrt:0~end:600にする)
                    int listCount = dataDtoList.Count;
                    int listEndCount = listCount - (BATCH_INSERT_MAX_NUM * i);
                    if (endNum > listEndCount)
                    {
                        endNum = listEndCount;
                    }

                    var insertDataDtoList = dataDtoList.GetRange(startNum, endNum);

                    var insertSql = " INSERT INTO diary (title, content, post_date) VALUES ";
                    int sqlRowNum = 1;
                    foreach (var insertDataDto in insertDataDtoList)
                    {
                        string valuesSql;
                        if (sqlRowNum == 1)
                        {
                            valuesSql = " (@title" + sqlRowNum + ", @content" + sqlRowNum + ", @post_date" + sqlRowNum + ") ";
                        }
                        else
                        {
                            valuesSql = ", (@title" + sqlRowNum + ", @content" + sqlRowNum + ", @post_date" + sqlRowNum + ") ";
                        }

                        insertSql += valuesSql;
                        sqlRowNum++;
                    }

                    using (var command = new MySqlCommand(insertSql, connection))
                    {
                        try
                        {
                            command.Connection = connection;
                            command.CommandText = insertSql;

                            int valRowNum = 1;
                            foreach (var insertDataDto in insertDataDtoList)
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
                }
                connection.Close();
            }
        }
    }
}
