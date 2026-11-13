import { FormControl, FormGroup } from '@angular/forms';
import { igualA, palavraPasseForte, urlHttp } from './validadores';

describe('I070 — validadores', () => {
  describe('urlHttp (RF-F001-16)', () => {
    const validar = (valor: string) => urlHttp(new FormControl(valor, { nonNullable: true }));

    for (const url of [
      'http://exemplo.pt',
      'https://www.linkedin.com/in/ana?x=1',
      'HTTPS://EXEMPLO.PT',
      '  https://exemplo.pt  ',
    ])
      it(`aceita «${url}»`, () => expect(validar(url)).toBeNull());

    for (const url of [
      'javascript:alert(1)',
      'exemplo.pt',
      'www.exemplo.pt',
      'ftp://exemplo.pt',
      'https:exemplo.pt',
      'https://',
      '   ',
    ])
      it(`recusa «${url}»`, () => expect(validar(url)).toEqual({ urlHttp: true }));

    it('deixa o campo vazio para o obrigatório', () => expect(validar('')).toBeNull());
  });

  describe('palavraPasseForte', () => {
    const validar = (valor: string) =>
      palavraPasseForte(new FormControl(valor, { nonNullable: true }));

    it('aceita letras com dígitos, incluindo acentos', () => {
      expect(validar('Palavra1')).toBeNull();
      expect(validar('ação2026')).toBeNull();
    });
    it('recusa sem dígitos ou sem letras', () => {
      expect(validar('sodeletras')).toEqual({ palavraPasseFraca: true });
      expect(validar('12345678')).toEqual({ palavraPasseFraca: true });
    });
    it('deixa o campo vazio para o obrigatório', () => {
      expect(validar('')).toBeNull();
    });
  });

  describe('igualA', () => {
    function confirmacao(password: string, repetida: string) {
      const controlo = new FormControl(repetida, [igualA('password')]);
      new FormGroup({ password: new FormControl(password), confirmacao: controlo });
      // O controlo é validado ao ser criado, antes de ter o formulário como pai.
      controlo.updateValueAndValidity();
      return controlo;
    }

    it('aceita valores iguais', () => {
      expect(confirmacao('abc', 'abc').valid).toBeTrue();
    });
    it('recusa valores diferentes', () => {
      expect(confirmacao('abc', 'abd').hasError('diferente')).toBeTrue();
    });
  });
});
