package pt.ipca.g15.campuscarpool.data.session
import android.annotation.SuppressLint
import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import com.google.gson.Gson
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec
import pt.ipca.g15.campuscarpool.domain.model.Sessao

// A extensão edit { } da KTX não devolve o resultado do commit(), de que dependem guardar e limpar.
@SuppressLint("UseKtx")
class AndroidSessionStore(context: Context) : SessionStore {
    private val prefs = context.getSharedPreferences("sessao_cifrada", Context.MODE_PRIVATE)
    private val gson = Gson()
    private val alias = "campuscarpool.sessao.v1"
    private fun chave(): SecretKey {
        val ks = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
        (ks.getKey(alias, null) as? SecretKey)?.let { return it }
        return KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore").apply {
            init(
                KeyGenParameterSpec.Builder(
                    alias,
                    KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT
                ).setBlockModes(
                    KeyProperties.BLOCK_MODE_GCM
                ).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).build()
            )
        }.generateKey()
    }
    override fun guardar(sessao: Sessao) {
        val cipher = Cipher.getInstance("AES/GCM/NoPadding").apply {
            init(Cipher.ENCRYPT_MODE, chave())
        }
        val bytes = cipher.doFinal(gson.toJson(sessao).toByteArray(Charsets.UTF_8))
        check(
            prefs.edit().putString(
                "iv",
                Base64.encodeToString(cipher.iv, Base64.NO_WRAP)
            ).putString("conteudo", Base64.encodeToString(bytes, Base64.NO_WRAP)).commit()
        ) {
            "Não foi possível guardar a sessão."
        }
    }
    override fun ler(): Sessao? {
        val conteudo = prefs.getString("conteudo", null) ?: return null
        return try {
            val iv = Base64.decode(requireNotNull(prefs.getString("iv", null)), Base64.NO_WRAP)
            val cipher = Cipher.getInstance("AES/GCM/NoPadding").apply {
                init(Cipher.DECRYPT_MODE, chave(), GCMParameterSpec(128, iv))
            }
            gson.fromJson(
                String(cipher.doFinal(Base64.decode(conteudo, Base64.NO_WRAP)), Charsets.UTF_8),
                Sessao::class.java
            )
        } catch (_: Exception) {
            limpar()
            null
        }
    }
    override fun limpar() {
        if (!prefs.edit().clear().commit()) {
            // Se a remoção falhar, inutiliza a chave para impedir recuperação do token.
            KeyStore.getInstance("AndroidKeyStore").apply {
                load(null)
                deleteEntry(alias)
            }
        }
    }
}
