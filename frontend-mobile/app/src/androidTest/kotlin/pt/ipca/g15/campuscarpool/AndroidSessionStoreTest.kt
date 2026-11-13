package pt.ipca.g15.campuscarpool

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Test
import org.junit.runner.RunWith
import pt.ipca.g15.campuscarpool.data.session.AndroidSessionStore
import pt.ipca.g15.campuscarpool.domain.model.Sessao

/** Só corre em dispositivo: o Android Keystore não existe nos testes em JVM. */
@RunWith(AndroidJUnit4::class)
class AndroidSessionStoreTest {
    private val contexto = ApplicationProvider.getApplicationContext<Context>()
    private val store = AndroidSessionStore(contexto)
    private val sessao = Sessao("token-sintetico", "A", 1_000, "https://exemplo.test/api/v1/")

    @After fun limpar() = store.limpar()

    @Test fun guardaELeOMesmoValor() {
        store.guardar(sessao)
        assertEquals(sessao, AndroidSessionStore(contexto).ler())
    }

    @Test fun oTokenNaoFicaEmTextoSimples() {
        store.guardar(sessao)
        val prefs = contexto.getSharedPreferences("sessao_cifrada", Context.MODE_PRIVATE)
        assertFalse(prefs.all.values.any { it.toString().contains("token-sintetico") })
    }

    @Test fun limparRemoveASessao() {
        store.guardar(sessao)
        store.limpar()
        assertNull(store.ler())
    }

    @Test fun conteudoAdulteradoEDescartado() {
        store.guardar(sessao)
        contexto.getSharedPreferences("sessao_cifrada", Context.MODE_PRIVATE).edit()
            .putString("conteudo", "AAAA").commit()
        assertNull(store.ler())
    }
}
