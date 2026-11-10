package pt.ipca.g15.campuscarpool.data.session
import pt.ipca.g15.campuscarpool.domain.model.Sessao
interface SessionStore {
    fun ler(): Sessao?
    fun guardar(sessao: Sessao)
    fun limpar()
}
