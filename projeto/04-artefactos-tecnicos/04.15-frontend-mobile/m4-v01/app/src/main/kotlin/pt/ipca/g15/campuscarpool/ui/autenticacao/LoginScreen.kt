package pt.ipca.g15.campuscarpool.ui.autenticacao
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import pt.ipca.g15.campuscarpool.R
@Composable fun LoginScreen(
    ocupado: Boolean,
    campos: Map<String, List<String>>,
    entrar: (String, String) -> Unit,
    voltar: () -> Unit
) {
    // O email sobrevive à rotação; a palavra-passe fica de fora do estado restaurável.
    var email by rememberSaveable { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var submeteu by remember { mutableStateOf(false) }
    LaunchedEffect(ocupado) { if (!ocupado && submeteu) password = "" }
    Column(
        Modifier.verticalScroll(rememberScrollState()).imePadding().padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(stringResource(R.string.conta_confirmada))
        OutlinedTextField(
            email,
            {
                email = it
            },
            Modifier.fillMaxWidth().testTag("login-email"),
            label = {
                Text(stringResource(R.string.email))
            },
            singleLine = true,
            enabled = !ocupado,
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Email
            )
        )
        (campos["email"] ?: campos["Email"]).orEmpty().forEach { Text(it) }
        OutlinedTextField(
            password,
            {
                password = it
            },
            Modifier.fillMaxWidth().testTag("login-password"),
            label = {
                Text(stringResource(R.string.password))
            },
            singleLine = true,
            enabled = !ocupado,
            visualTransformation = PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Password
            )
        )
        (campos["password"] ?: campos["Password"]).orEmpty().forEach { Text(it) }
        Button(
            onClick = {
                submeteu = true
                entrar(email, password)
            },
            enabled =
            !ocupado && email.isNotBlank() && password.isNotBlank(),
            modifier = Modifier.testTag("login-submit")
        ) {
            Text(stringResource(if (ocupado) R.string.a_entrar else R.string.entrar))
        }
        TextButton(onClick = voltar, enabled = !ocupado) { Text(stringResource(R.string.voltar)) }
    }
}
