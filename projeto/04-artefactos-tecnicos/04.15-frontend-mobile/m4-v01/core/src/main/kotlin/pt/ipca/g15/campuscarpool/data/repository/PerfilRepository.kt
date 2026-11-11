package pt.ipca.g15.campuscarpool.data.repository
import pt.ipca.g15.campuscarpool.data.remote.ApiService
import pt.ipca.g15.campuscarpool.data.remote.ErrosApi
import pt.ipca.g15.campuscarpool.domain.model.AtualizarPerfilPedido
import pt.ipca.g15.campuscarpool.domain.model.NovaLigacaoPedido
class PerfilRepository(private val api: ApiService) {
    suspend fun obterProprio() = ErrosApi.executar { api.perfilProprio() }

    suspend fun atualizar(nome: String, curso: String, instituicao: String) = ErrosApi.executar {
        api.atualizarPerfil(
            AtualizarPerfilPedido(nome.trim(), opcional(curso), opcional(instituicao))
        )
    }

    suspend fun obterPublico(id: String) = ErrosApi.executar { api.perfilPublico(id) }

    suspend fun adicionarLigacao(url: String, descricao: String) = ErrosApi.executar {
        api.adicionarLigacao(NovaLigacaoPedido(url.trim(), opcional(descricao)))
    }

    suspend fun removerLigacao(id: String) = ErrosApi.executarSemCorpo { api.removerLigacao(id) }

    private fun opcional(valor: String) = valor.trim().ifEmpty { null }
}
