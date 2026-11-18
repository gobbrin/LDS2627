package pt.ipca.g15.campuscarpool.ui.comunidade
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
@Composable fun ComunidadeScreen() {
    var pagina by rememberSaveable { mutableStateOf("Pesquisa") }
    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Text("Campus Carpool", style = MaterialTheme.typography.headlineMedium)
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(4.dp)) {
            Botao("Pesquisar") {
                pagina =
                    "Pesquisa"
            }
        }
        Botao("Participações") { pagina = "Participações" }
        Botao("Mensagens") { pagina = "Mensagens" }
        Botao("Notificações") { pagina = "Notificações" }
        // MENU
        when (pagina) {
            "Pesquisa" -> PesquisaScreen()
            "Participações" -> ParticipacoesScreen()
            "Mensagens" -> MensagensScreen()
            "Notificações" -> NotificacoesScreen()
            // PAGINAS
        }
    }
}
