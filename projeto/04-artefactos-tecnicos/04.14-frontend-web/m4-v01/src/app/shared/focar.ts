import { afterNextRender, Injector } from '@angular/core';

/** Devolve o foco a um elemento quando o controlo que o tinha desaparece do ecrã. */
export function focarDepoisDeDesenhar(injector: Injector, id: string) {
  afterNextRender(() => document.getElementById(id)?.focus(), { injector });
}
