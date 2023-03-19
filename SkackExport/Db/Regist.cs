using System;
using System.Data.SqlClient;
using System.Runtime.InteropServices.ComTypes;
using MySqlConnector;
using SlackExport.Dto;

namespace SlackExport.Db
{
    public class Regist
    {
        public bool insert(DataDto dataDto)
        {
            // 接続文字列
            // 設定ファイル等の外部パラメータ化したいが、とりあえず定数にしておく。
            // 下記にDBの接続情報をセットする。
            var connectionString = "server=127.0.0.1;port=3306;uid=xxx;pwd=xxx;database=DiarySample";

            // 実行するSQL
            var sql = "INSERT INTO diary (id, title, content, post_date, update_date)  SELECT MAX(id) + 1, @title, @content, @post_date, @update_date FROM diary";

            using (var connection = new MySqlConnection(connectionString))
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
                        command.Parameters.AddWithValue("@title", dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user);
                        command.Parameters.AddWithValue("@content", dataDto.message);
                        command.Parameters.AddWithValue("@post_date", DateTime.Now);
                        command.Parameters.AddWithValue("@update_date", DateTime.Now);

                        var result = command.ExecuteNonQuery();
                        if (result != 1)
                        {
                            Console.WriteLine("insertエラー");
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return true;
        }
    }
}
