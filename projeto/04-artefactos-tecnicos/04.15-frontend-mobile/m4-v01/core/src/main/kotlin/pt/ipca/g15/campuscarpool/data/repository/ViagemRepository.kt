package pt.ipca.g15.campuscarpool.data.repository

import pt.ipca.g15.campuscarpool.data.remote.ApiService
import pt.ipca.g15.campuscarpool.data.remote.ErrosApi
import pt.ipca.g15.campuscarpool.domain.model.DadosViagem

class ViagemRepository(private val api: ApiService) {
    suspend fun criar(dados: DadosViagem) = ErrosApi.executar { api.criarViagem(dados) }

    suspend fun listarMinhas() = ErrosApi.executar { api.minhasViagens() }

    suspend fun obterDetalhe(id: String) = ErrosApi.executar { api.viagem(id) }

    suspend fun alterar(id: String, dados: DadosViagem) =
        ErrosApi.executarSemCorpo { api.alterarViagem(id, dados) }

    suspend fun indisponibilizar(id: String) =
        ErrosApi.executarSemCorpo { api.indisponibilizarViagem(id) }
}
