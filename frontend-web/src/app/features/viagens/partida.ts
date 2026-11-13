import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** RNF-FIA-04: a interface usa a hora de Portugal continental, seja qual for o fuso do browser. */
const FUSO_PORTUGAL = 'Europe/Lisbon';
const UM_DIA_MS = 24 * 60 * 60 * 1000;

const FORMATO_PARTIDA = new Intl.DateTimeFormat('pt-PT', {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: FUSO_PORTUGAL,
});

const FORMATO_PARTES = new Intl.DateTimeFormat('en-CA', {
  timeZone: FUSO_PORTUGAL,
  hourCycle: 'h23',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
});

function partesEmPortugal(instante: number) {
  const partes = Object.fromEntries(
    FORMATO_PARTES.formatToParts(instante).map(({ type, value }) => [type, value]),
  );
  return partes as Record<'year' | 'month' | 'day' | 'hour' | 'minute', string>;
}

/** Diferença entre a hora de Portugal e o UTC nesse instante, em milissegundos. */
function desvioDePortugal(instante: number): number {
  const { year, month, day, hour, minute } = partesEmPortugal(instante);
  const relogio = Date.UTC(+year, +month - 1, +day, +hour, +minute);
  return relogio - instante + (instante % 60000);
}

/**
 * A API só aceita datas em UTC com `Z`; o formulário trabalha na hora de Portugal. Nas mudanças
 * de hora segue a regra do mobile: uma hora repetida (outubro) conta como a primeira e uma hora
 * que não existe (março) avança a diferença.
 */
export function paraPartidaUtc(data: string, hora: string): string {
  const relogio = Date.parse(`${data}T${hora}Z`);
  const candidatos = [
    relogio - desvioDePortugal(relogio - UM_DIA_MS),
    relogio - desvioDePortugal(relogio + UM_DIA_MS),
  ];
  const instante = candidatos.find((c) => desvioDePortugal(c) === relogio - c) ?? candidatos[0];
  return new Date(instante).toISOString();
}

/** Valores dos campos `date` e `time`, na hora de Portugal, para uma partida devolvida pela API. */
export function paraCamposPortugal(partidaEm: string): { data: string; hora: string } {
  const { year, month, day, hour, minute } = partesEmPortugal(Date.parse(partidaEm));
  return { data: `${year}-${month}-${day}`, hora: `${hour}:${minute}` };
}

export function formatarPartida(partidaEm: string): string {
  return FORMATO_PARTIDA.format(new Date(partidaEm));
}

/** Texto curto da rota calculada; vazio se a API ainda não tiver distância nem duração. */
export function formatarRota(distanciaKm: number | null, duracaoMinutos: number | null): string {
  const partes: string[] = [];
  if (distanciaKm !== null)
    partes.push(
      `${distanciaKm.toLocaleString('pt-PT', { maximumFractionDigits: 1, minimumFractionDigits: 1 })} km`,
    );
  if (duracaoMinutos !== null) {
    const horas = Math.floor(duracaoMinutos / 60);
    const minutos = duracaoMinutos % 60;
    partes.push(horas ? `cerca de ${horas} h ${minutos} min` : `cerca de ${minutos} min`);
  }
  return partes.join(' · ');
}

/** RF-F002-02, para o campo da hora. Os campos por preencher ficam para `Validators.required`. */
export function partidaFutura(campoData: string): ValidatorFn {
  return (controlo: AbstractControl<string>): ValidationErrors | null => {
    const data: unknown = controlo.parent?.get(campoData)?.value;
    if (!data || !controlo.value) return null;
    return Date.parse(paraPartidaUtc(String(data), controlo.value)) > Date.now()
      ? null
      : { passada: true };
  };
}
