import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** P-08: pelo menos uma letra e um dígito. O comprimento fica para `Validators.minLength`. */
export function palavraPasseForte(controlo: AbstractControl<string>): ValidationErrors | null {
  const valor = controlo.value;
  const valida = !valor || (/\p{L}/u.test(valor) && /\p{Nd}/u.test(valor));
  return valida ? null : { palavraPasseFraca: true };
}

/** RF-F001-16: URL absoluto `http` ou `https`. O vazio fica para `Validators.required`. */
export function urlHttp(controlo: AbstractControl<string>): ValidationErrors | null {
  if (!controlo.value) return null;
  const valor = controlo.value.trim();
  // A expressão exige as duas barras: o `URL` também aceitaria «https:exemplo.pt».
  const valida = /^https?:\/\//i.test(valor) && URL.canParse(valor);
  return valida ? null : { urlHttp: true };
}

/** Compara com outro campo do mesmo formulário, como a confirmação da palavra-passe. */
export function igualA(campo: string): ValidatorFn {
  return (controlo) => {
    const outro: unknown = controlo.parent?.get(campo)?.value;
    return controlo.value === outro ? null : { diferente: true };
  };
}
