package pt.ipca.g15.campuscarpool.ui.inicio
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import pt.ipca.g15.campuscarpool.R
@Composable fun InicioScreen(
    autenticado: Boolean,
    entrar: () -> Unit,
    registar: () -> Unit,
    perfil: () -> Unit,
    viagens: () -> Unit,
    sair: () -> Unit,
    ocupado: Boolean
) {
    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(24.dp)
    ) {
        Text(stringResource(R.string.app_name), style = MaterialTheme.typography.titleLarge)
        Text(stringResource(R.string.titulo_inicio), style = MaterialTheme.typography.headlineLarge)
        Text(stringResource(R.string.descricao_inicio))
        if (autenticado) {
            Button(onClick = perfil) { Text(stringResource(R.string.perfil)) }
            Button(onClick = viagens) { Text(stringResource(R.string.minhas_viagens)) }
            TextButton(onClick = sair, enabled = !ocupado) {
                Text(stringResource(if (ocupado) R.string.a_terminar else R.string.sair))
            }
        } else {
            Button(onClick = registar) { Text(stringResource(R.string.criar_conta)) }
            TextButton(onClick = entrar) { Text(stringResource(R.string.entrar)) }
        }
    }
}
