package pt.ipca.g15.campuscarpool.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginViewModel
import pt.ipca.g15.campuscarpool.ui.autenticacao.RegistoViewModel
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilViewModel

class FabricaViewModels(
    private val sessao: SessaoManager,
    private val autenticacao: AutenticacaoRepository,
    private val perfis: PerfilRepository
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T = when (modelClass) {
        LoginViewModel::class.java -> LoginViewModel(sessao, autenticacao)
        RegistoViewModel::class.java -> RegistoViewModel(autenticacao)
        PerfilViewModel::class.java -> PerfilViewModel(perfis)
        else -> throw IllegalArgumentException("ViewModel desconhecido: ${modelClass.name}")
    } as T
}
