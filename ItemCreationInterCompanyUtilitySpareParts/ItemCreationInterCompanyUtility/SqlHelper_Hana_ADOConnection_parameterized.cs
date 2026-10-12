// =============================================================================
// ADD THIS OVERLOAD inside the SqlHelper class in SqlHelper.cs
// (e.g. right after the existing Hana_ADOConnection(string _Query) method,
//  around line 2430). Requires:  using Sap.Data.Hana;
// =============================================================================

/// <summary>
/// Parameterized HANA query. Use this for any query that includes user input
/// (login, search boxes, etc.) so values can never be interpreted as SQL.
/// Uses local connection/adapter objects so a failed call cannot return a
/// stale DataTable from a previous query, and rethrows on error so the caller
/// can react instead of silently receiving an empty/old result.
/// </summary>
public static DataTable Hana_ADOConnection(string _Query, HanaParameter[] parameters)
{
    DataSet ds = LoadCompanySetting();
    string ServerName    = ds.Tables[0].Rows[0]["ServerName"].ToString().Trim();
    string UserID        = ds.Tables[0].Rows[0]["SourceUserID"].ToString().Trim();
    string Password      = ds.Tables[0].Rows[0]["Password"].ToString().Trim();
    string CurrentSchema = ds.Tables[0].Rows[0]["CurrentSchema"].ToString().Trim();

    string connStr = "Server=" + ServerName +
                     ";UserID=" + UserID +
                     ";Password=" + Password +
                     ";Current Schema=" + CurrentSchema;

    DataTable result = new DataTable();

    using (HanaConnection conn = new HanaConnection(connStr))
    using (HanaCommand cmd = new HanaCommand(_Query, conn))
    {
        cmd.CommandType = CommandType.Text;
        if (parameters != null)
            cmd.Parameters.AddRange(parameters);

        conn.Open();
        using (HanaDataAdapter da = new HanaDataAdapter(cmd))
        {
            da.Fill(result);
        }
    }

    return result;
}
