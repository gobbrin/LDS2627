package pt.ipca.g15.campuscarpool.data.repository
import pt.ipca.g15.campuscarpool.data.remote.ApiService
import pt.ipca.g15.campuscarpool.data.remote.ErrosApi
class PerfilRepository(private val api: ApiService) {
    suspend fun obterProprio() = ErrosApi.executar { api.perfilProprio() }
}
