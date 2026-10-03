export type DeviceFace = 'frente' | 'tras' | 'esquerda' | 'direita' | 'superior' | 'inferior';
export type DamagePoint = { face: DeviceFace; x: number; y: number; componente?: string; defeito?: string; defeitos?: string[]; observacao?: string };
export type DeviceComponent = { id: string; label: string; face: DeviceFace; x: number; y: number; defects: string[] };

export const deviceFaces: { key: DeviceFace; label: string }[] = [
  { key: 'frente', label: 'Frente' }, { key: 'tras', label: 'Traseira' },
  { key: 'esquerda', label: 'Esquerda' }, { key: 'direita', label: 'Direita' },
  { key: 'superior', label: 'Superior' }, { key: 'inferior', label: 'Inferior' },
];
export const deviceComponents: DeviceComponent[] = [
  { id: 'tela', label: 'Tela', face: 'frente', x: .5, y: .53, defects: ['Trincada / quebrada', 'Sem imagem', 'Manchas ou linhas', 'Touch não responde', 'Touch com falhas'] },
  { id: 'camera-frontal', label: 'Câmera frontal', face: 'frente', x: .67, y: .085, defects: ['Sem imagem', 'Imagem embaçada', 'Falha no foco', 'Lente danificada'] },
  { id: 'face-id', label: 'Face ID', face: 'frente', x: .41, y: .085, defects: ['Não reconhece o rosto', 'Indisponível', 'Falha intermitente'] },
  { id: 'botoes-volume', label: 'Botões de volume', face: 'esquerda', x: .5, y: .37, defects: ['Não respondem', 'Travados', 'Danificados', 'Falha intermitente'] },
  { id: 'botao-lateral', label: 'Botão lateral', face: 'direita', x: .5, y: .39, defects: ['Não responde', 'Travado', 'Danificado', 'Falha intermitente'] },
  { id: 'conector', label: 'Conector de carga', face: 'inferior', x: .5, y: .5, defects: ['Não carrega', 'Mau contato', 'Conector danificado', 'Não transfere dados', 'Oxidação aparente'] },
  { id: 'camera-traseira', label: 'Câmera traseira', face: 'tras', x: .24, y: .18, defects: ['Sem imagem', 'Imagem embaçada', 'Falha no foco', 'Lente trincada', 'Vibração na imagem'] },
  { id: 'traseira', label: 'Tampa traseira', face: 'tras', x: .5, y: .72, defects: ['Trincada / quebrada', 'Riscada', 'Ausente', 'Descolada', 'Amassada'] },
  { id: 'bateria', label: 'Bateria (interna)', face: 'tras', x: .5, y: .48, defects: ['Estufada', 'Vida útil reduzida', 'Descarrega rápido', 'Não mantém carga', 'Aquecimento excessivo'] },
];

export function damageLabel(point: DamagePoint): string {
  return deviceComponents.find(component => component.id === point.componente)?.label
    ?? `Avaria na face ${deviceFaces.find(face => face.key === point.face)?.label.toLocaleLowerCase('pt-BR') ?? point.face}`;
}

export function pointDefects(point: DamagePoint): string[] {
  return point.defeitos?.length ? point.defeitos : point.defeito ? [point.defeito] : [];
}
