package pt.ipca.g15.campuscarpool.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.data.repository.ViagemRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginViewModel
import pt.ipca.g15.campuscarpool.ui.autenticacao.RegistoViewModel
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilViewModel
import pt.ipca.g15.campuscarpool.ui.viagens.MinhasViagensViewModel
import pt.ipca.g15.campuscarpool.ui.viagens.ViagemFormViewModel

class FabricaViewModels(
    private val sessao: SessaoManager,
    private val autenticacao: AutenticacaoRepository,
    private val perfis: PerfilRepository,
    private val viagens: ViagemRepository
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T = when (modelClass) {
        LoginViewModel::class.java -> LoginViewModel(sessao, autenticacao)
        RegistoViewModel::class.java -> RegistoViewModel(autenticacao)
        PerfilViewModel::class.java -> PerfilViewModel(perfis)
        MinhasViagensViewModel::class.java -> MinhasViagensViewModel(viagens)
        ViagemFormViewModel::class.java ->
            ViagemFormViewModel(viagens, { sessao.estado.value?.utilizadorId })
        else -> throw IllegalArgumentException("ViewModel desconhecido: ${modelClass.name}")
    } as T
}
