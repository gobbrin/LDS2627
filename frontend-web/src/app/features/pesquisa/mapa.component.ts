import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  effect,
  input,
  signal,
  viewChild,
} from '@angular/core';
import * as L from 'leaflet';
import { PontoDaRota } from './pesquisa-api';

/** Nomes dos tipos de ponto devolvidos pela API, para a lista e para as etiquetas do mapa. */
const NOME_DO_TIPO: Record<string, string> = {
  Origem: 'Partida',
  Recolha: 'Recolha de passageiro',
  DestinoPassageiro: 'Saída de passageiro',
  DestinoViagem: 'Destino',
};

/**
 * Mapa da rota (RF-F008-03, RF-F008-07): marcadores numerados pela ordem dos pontos e a linha
 * devolvida pela API. A lista ordenada por baixo do mapa repete os pontos em texto, para quem
 * não vê o mapa ou quando o fundo do mapa não carrega.
 */
@Component({
  selector: 'app-mapa',
  template: `<div
      #mapa
      class="mapa"
      style="height: 320px"
      role="img"
      aria-label="Mapa com a rota e os pontos da viagem"
    ></div>
    @if (aproximada()) {
      <p class="muted">A linha do mapa é aproximada: liga os pontos em reta.</p>
    }
    @if (fundoFalhou()) {
      <p role="status">Não foi possível carregar o fundo do mapa. Consulta a lista de pontos.</p>
    }
    <ol>
      @for (ponto of pontos(); track ponto.ordem) {
        <li>{{ nomeDoTipo(ponto) }}: {{ ponto.local }}</li>
      }
    </ol>`,
})
export class MapaComponent implements AfterViewInit, OnDestroy {
  /** Pontos da rota, já pela ordem da viagem. */
  readonly pontos = input<PontoDaRota[]>([]);
  /** Linha a desenhar, em pares [latitude, longitude]. */
  readonly geometria = input<number[][]>([]);
  /** Verdadeiro quando a linha são só retas entre os pontos. */
  readonly aproximada = input(false);

  readonly fundoFalhou = signal(false);
  private readonly elemento = viewChild.required<ElementRef<HTMLDivElement>>('mapa');
  private mapa?: L.Map;
  private camada?: L.LayerGroup;

  constructor() {
    // Volta a desenhar sempre que os pontos ou a linha mudam.
    effect(() => this.desenhar(this.pontos(), this.geometria()));
  }

  /** Cria o mapa depois de o elemento existir; o fundo vem do OpenStreetMap. */
  ngAfterViewInit() {
    this.mapa = L.map(this.elemento().nativeElement).setView([41.37, -8.2], 10);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 19,
    })
      .on('tileerror', () => this.fundoFalhou.set(true))
      .addTo(this.mapa);
    this.desenhar(this.pontos(), this.geometria());
  }

  ngOnDestroy() {
    this.mapa?.remove();
  }

  nomeDoTipo(ponto: PontoDaRota) {
    return NOME_DO_TIPO[ponto.tipo] ?? ponto.tipo;
  }

  /** Substitui a linha e os marcadores e ajusta o enquadramento a todos eles. */
  private desenhar(pontos: PontoDaRota[], geometria: number[][]) {
    if (!this.mapa) return;
    this.camada?.remove();
    this.camada = L.layerGroup().addTo(this.mapa);
    const posicoes: L.LatLngTuple[] = [];

    const linha = geometria.filter(coordenadaValida).map(([lat, lng]): L.LatLngTuple => [lat, lng]);
    if (linha.length >= 2) {
      L.polyline(linha, { weight: 4 }).addTo(this.camada);
      posicoes.push(...linha);
    }

    for (const ponto of pontos) {
      if (!coordenadaValida([ponto.latitude, ponto.longitude])) continue;
      const posicao: L.LatLngTuple = [ponto.latitude, ponto.longitude];
      // A etiqueta vai como texto (textContent), nunca como HTML: o local foi escrito por um utilizador.
      const etiqueta = document.createElement('span');
      etiqueta.textContent = `${ponto.ordem + 1}. ${this.nomeDoTipo(ponto)}: ${ponto.local}`;
      L.circleMarker(posicao, { radius: 8 }).bindTooltip(etiqueta).addTo(this.camada);
      posicoes.push(posicao);
    }

    if (posicoes.length)
      this.mapa.fitBounds(L.latLngBounds(posicoes), { padding: [20, 20], maxZoom: 15 });
  }
}

/** Aceita só pares [latitude, longitude] numéricos e dentro dos limites da Terra. */
function coordenadaValida(par: number[]): boolean {
  const [lat, lng] = par;
  return (
    par.length >= 2 &&
    Number.isFinite(lat) &&
    Number.isFinite(lng) &&
    Math.abs(lat) <= 90 &&
    Math.abs(lng) <= 180
  );
}
