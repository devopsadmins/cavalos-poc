using CavalosPOC.Models;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace CavalosPOC.Data;

public class CavaloRepository
{
    private readonly string _connectionString;
    private readonly ILogger<CavaloRepository> _logger;

    public CavaloRepository(string connectionString, ILogger<CavaloRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<List<CavaloRegInfo>> ObterCavalosPorNomeAsync(string iNome)
    {
        var resultado = new List<CavaloRegInfo>();

        var nomeUpper = iNome?.ToUpperInvariant() ?? string.Empty;

        const string sql = @"
                SELECT 
                    COALESCE(T1.REG_REG, C1.CAV_REG) AS REG, 
                    C1.CAV_NOM, 
                    C1.CAV_ASTERISCO 
                FROM ABCCA.CAVALO C1
                LEFT JOIN ABCCA.REG T1 ON C1.CAV_SEQ = T1.REG_CAV_SEQ
                WHERE UPPER(C1.CAV_NOM_SACEN) LIKE '%' || :iNome || '%'
                ORDER BY C1.CAV_NOM";

        _logger.LogDebug("Executando SQL: {Sql} | Parâmetros: {{iNome: {INome}}}", sql, nomeUpper);

        var sqlComParametro = sql.Replace(":iNome", $"'{nomeUpper}'");
        _logger.LogDebug("SQL com parâmetro: {Sql}", sqlComParametro);

        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = new OracleCommand(sql, connection))
            {
                command.BindByName = true;
                var param = new OracleParameter("iNome", OracleDbType.Varchar2, nomeUpper, ParameterDirection.Input);
                command.Parameters.Add(param);

                _logger.LogDebug("Parâmetro Oracle: ParameterName={ParameterName}, OracleDbType={OracleDbType}, Value={Value}", 
                    param.ParameterName, param.OracleDbType, param.Value);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    int idxReg = reader.GetOrdinal("REG");
                    int idxCavNom = reader.GetOrdinal("CAV_NOM");
                    int idxCavAsterisco = reader.GetOrdinal("CAV_ASTERISCO");

                    while (await reader.ReadAsync())
                    {
                        var item = new CavaloRegInfo
                        {
                            Reg = reader.IsDBNull(idxReg) ? null : reader.GetString(idxReg),
                            CavNom = reader.IsDBNull(idxCavNom) ? null : reader.GetString(idxCavNom),
                            CavAsterisco = reader.IsDBNull(idxCavAsterisco) ? null : reader.GetString(idxCavAsterisco)
                        };

                        resultado.Add(item);
                    }
                }
            }
        }

        _logger.LogInformation("Consulta retornou {Count} registros para nome={Nome}", resultado.Count, iNome);
        return resultado;
    }
}
