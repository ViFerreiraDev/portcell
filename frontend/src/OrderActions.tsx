import { useEffect, useState } from 'react';
import { Alert, Box, Button, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { request, type Ordem, type Orcamento } from './api';
import type { Service } from './ServiceAdministration';
import { damageLabel, pointDefects, type DamagePoint } from './device-components';

type Piece = { id: string; sku: string; descricao: string; saldo: number };
type Detail = { timeline: { id: string; evento: string; detalhes?: string; createdAt: string }[]; checklist: { id: string; itemNome: string; resposta: string }[]; pontosAvaria: DamagePoint[]; reparos: { id: string; inicio: string; fim?: string; servicosRealizados?: string; observacoes?: string }[]; testes: { id: string; aprovado: boolean; resultado: string; createdAt: string }[]; respostasTeste: { id: string; testeFinalId: string; itemNome: string; resultado: string }[]; reparoEmAndamento: boolean };
type Payments = { total: number; saldo: number; statusFinanceiro: string; pagamentos: { id: string; valor: number; forma: string; estornoDeId?: string }[] };
type QuoteDetail = { itens: { tipo: string; descricao: string }[] };
type TestItem = { id: string; nome: string; ativo: boolean };

export function OrderActions({ order, token, profile, latest, onStatus }: { order: Ordem; token: string; profile: string; latest?: Orcamento; onStatus: (status: string) => void }) {
  const [pieces, setPieces] = useState<Piece[]>([]);
  const [testItems, setTestItems] = useState<TestItem[]>([]); const [testAnswers, setTestAnswers] = useState<Record<string, string>>({});
  const [detail, setDetail] = useState<Detail | null>(null);
  const [payments, setPayments] = useState<Payments | null>(null);
  const [services, setServices] = useState<string[]>([]);
  const [pieceId, setPieceId] = useState(''); const [quantity, setQuantity] = useState('1');
  const [repairNote, setRepairNote] = useState(''); const [repairObservations, setRepairObservations] = useState(''); const [testResult, setTestResult] = useState('');
  const [paymentValue, setPaymentValue] = useState(''); const [paymentMethod, setPaymentMethod] = useState('Pix');
  const [refundReason, setRefundReason] = useState('');
  const [recipient, setRecipient] = useState('');
  const [warranties, setWarranties] = useState<Record<string, { dias: string; condicoes: string }>>({});
  const [deviceAccess, setDeviceAccess] = useState(''); const [visibleAccess, setVisibleAccess] = useState('');
  const [pauseReason, setPauseReason] = useState('');
  const [error, setError] = useState('');
  const canRepair = ['Administrador', 'Gerente', 'Tecnico'].includes(profile);

  async function reload() {
    try {
      const [d, p, pay, tests, catalog] = await Promise.all([
        request<Detail>(`/api/ordens-servico/${order.id}`, token), request<Piece[]>('/api/pecas', token),
        ['Administrador', 'Gerente', 'Atendente'].includes(profile)
          ? request<Payments>(`/api/ordens-servico/${order.id}/pagamentos`, token) : Promise.resolve(null),
        request<TestItem[]>('/api/configuracoes/testes', token),
        request<Service[]>('/api/servicos', token),
      ]);
      setDetail(d); setPieces(p); setPayments(pay); setTestItems(tests.filter(x => x.ativo));
      if (latest) {
        const quote = await request<QuoteDetail>(`/api/orcamentos/${latest.id}`, token);
        const names = quote.itens.filter(x => x.tipo === 'Servico').map(x => x.descricao);
        setServices(names);
        setWarranties(Object.fromEntries(names.map(name => {
          const service = catalog.find(x => x.nome === name);
          return [name, { dias: String(service?.garantiaDias ?? 90), condicoes: service?.condicoesGarantia ?? 'Garantia do serviço realizado.' }];
        })));
      }
      setError('');
    } catch (error) { setError(String(error)); }
  }
  useEffect(() => { setVisibleAccess(''); setDeviceAccess(''); void reload(); }, [order.id, order.status, latest?.id]);
  async function act(path: string, body?: unknown, status?: string) {
    try {
      await request(path, token, 'POST', body);
      if (status) onStatus(status);
      await reload();
    } catch (error) { setError(String(error)); }
  }
  async function changeStatus(status: string, justificativa?: string) {
    try {
      await request(`/api/ordens-servico/${order.id}/status`, token, 'PATCH', { status, justificativa });
      setPauseReason(''); onStatus(status); await reload();
    } catch (error) { setError(String(error)); }
  }
  return <Paper sx={{ p: 3 }}><Stack spacing={2}>
    <Typography variant="h6">Execução e fechamento</Typography>
    {error && <Alert severity="error">{error}</Alert>}
    {order.acessoNecessario && ['Administrador', 'Tecnico'].includes(profile) && !['Entregue', 'Cancelado', 'SemReparo', 'Irreparavel'].includes(order.status) && <Box component="form" onSubmit={async event => { event.preventDefault(); try { await request(`/api/ordens-servico/${order.id}/acesso-dispositivo`, token, 'PUT', { credencial: deviceAccess }); setDeviceAccess(''); setVisibleAccess(''); await reload(); } catch (error) { setError(String(error)); } }}><Stack spacing={1}><Typography>Acesso ao dispositivo necessário para este reparo</Typography><TextField label="PIN ou senha temporária" type="password" value={deviceAccess} onChange={event => setDeviceAccess(event.target.value)} required /><Stack direction="row" spacing={1}><Button type="submit">Guardar acesso</Button><Button onClick={async () => { try { const result = await request<{ credencial: string }>(`/api/ordens-servico/${order.id}/acesso-dispositivo`, token); setVisibleAccess(result.credencial); } catch (error) { setError(String(error)); } }}>Consultar acesso</Button></Stack>{visibleAccess && <Alert severity="warning" onClose={() => setVisibleAccess('')}>Acesso: {visibleAccess}</Alert>}</Stack></Box>}
    {canRepair && order.status === 'Aprovado' && <Button variant="contained" onClick={() => act(`/api/ordens-servico/${order.id}/reparo/iniciar`, undefined, 'EmReparo')}>Iniciar reparo</Button>}
    {canRepair && ['Aprovado', 'EmReparo'].includes(order.status) && <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}><TextField label="Peça ou insumo pendente" value={pauseReason} onChange={event => setPauseReason(event.target.value)} size="small" /><Button disabled={!pauseReason.trim()} onClick={() => void changeStatus('AguardandoPeca', pauseReason)}>Aguardar peça</Button></Stack>}
    {canRepair && order.status === 'AguardandoPeca' && detail && <Button variant="contained" onClick={() => void changeStatus(detail.reparoEmAndamento ? 'EmReparo' : 'Aprovado')}>Retomar atendimento</Button>}
    {canRepair && order.status === 'EmReparo' && <>
      <Box component="form" onSubmit={event => { event.preventDefault(); void act(`/api/ordens-servico/${order.id}/pecas`, { pecaId: pieceId, quantidade: Number(quantity) }); }}><Stack spacing={1}>
        <Typography>Consumo de peça</Typography><TextField select label="Peça" value={pieceId} onChange={event => setPieceId(event.target.value)} required>{pieces.map(x => <MenuItem key={x.id} value={x.id}>{x.sku} · {x.descricao} (saldo: {x.saldo})</MenuItem>)}</TextField>
        <TextField label="Quantidade" type="number" inputProps={{ min: 0.001, step: 0.001 }} value={quantity} onChange={event => setQuantity(event.target.value)} required /><Button type="submit">Registrar consumo</Button>
      </Stack></Box>
      <Box component="form" onSubmit={event => { event.preventDefault(); void act(`/api/ordens-servico/${order.id}/reparo/concluir`, { servicosRealizados: repairNote, observacoes: repairObservations }, 'EmTestes'); }}><Stack spacing={1}><TextField label="Serviços realizados" multiline required value={repairNote} onChange={event => setRepairNote(event.target.value)} /><TextField label="Observações técnicas" multiline value={repairObservations} onChange={event => setRepairObservations(event.target.value)} /><Button type="submit" variant="contained">Concluir reparo</Button></Stack></Box>
    </>}
    {canRepair && order.status === 'EmTestes' && <Box component="form" onSubmit={event => { event.preventDefault(); void act(`/api/ordens-servico/${order.id}/testes`, { aprovado: true, resultado: testResult, itens: testItems.map(x => ({ itemId: x.id, resultado: testAnswers[x.id] })) }, 'ProntoParaRetirada'); }}><Stack spacing={1}>{testItems.map(x => <TextField key={x.id} select label={x.nome} value={testAnswers[x.id] ?? ''} onChange={event => setTestAnswers({ ...testAnswers, [x.id]: event.target.value })} required>{['OK', 'Falha', 'NaoAplicavel'].map(value => <MenuItem key={value} value={value}>{value === 'NaoAplicavel' ? 'Não aplicável' : value}</MenuItem>)}</TextField>)}<TextField label="Resultado dos testes" multiline required value={testResult} onChange={event => setTestResult(event.target.value)} /><Stack direction="row" spacing={1}><Button type="submit" variant="contained" disabled={Object.values(testAnswers).includes('Falha')}>Aprovar testes</Button><Button color="error" type="button" disabled={!testResult.trim() || testItems.some(x => !testAnswers[x.id])} onClick={() => act(`/api/ordens-servico/${order.id}/testes`, { aprovado: false, resultado: testResult, itens: testItems.map(x => ({ itemId: x.id, resultado: testAnswers[x.id] })) }, 'EmReparo')}>Reprovar</Button></Stack></Stack></Box>}
    {payments && <Typography>Financeiro: {payments.statusFinanceiro}. Pagamento: {payments.pagamentos.reduce((sum, x) => sum + x.valor, 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} de {payments.total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}. Saldo: {payments.saldo.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</Typography>}
    {payments && ['Administrador', 'Gerente'].includes(profile) && payments.pagamentos.some(x => x.valor > 0 && !payments.pagamentos.some(y => y.estornoDeId === x.id)) && <Stack spacing={1}><TextField label="Motivo do estorno" value={refundReason} onChange={event => setRefundReason(event.target.value)} />{payments.pagamentos.filter(x => x.valor > 0 && !payments.pagamentos.some(y => y.estornoDeId === x.id)).map(x => <Button key={x.id} color="error" disabled={!refundReason.trim()} onClick={() => { void act(`/api/ordens-servico/${order.id}/pagamentos/${x.id}/estorno`, { justificativa: refundReason }); setRefundReason(''); }}>Estornar {x.valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} ({x.forma})</Button>)}</Stack>}
    {['Administrador', 'Gerente', 'Atendente'].includes(profile) && ['Aprovado', 'EmReparo', 'EmTestes', 'ProntoParaRetirada'].includes(order.status) && <Box component="form" onSubmit={event => { event.preventDefault(); void act(`/api/ordens-servico/${order.id}/pagamentos`, { valor: Number(paymentValue), forma: paymentMethod }); setPaymentValue(''); }}><Stack spacing={1}><Typography>Registrar pagamento</Typography><TextField label="Valor" type="number" inputProps={{ min: 0.01, step: 0.01 }} required value={paymentValue} onChange={event => setPaymentValue(event.target.value)} /><TextField select label="Forma" value={paymentMethod} onChange={event => setPaymentMethod(event.target.value)}>{['Pix', 'Dinheiro', 'Cartao', 'Transferencia'].map(x => <MenuItem key={x} value={x}>{x}</MenuItem>)}</TextField><Button type="submit">Registrar pagamento</Button></Stack></Box>}
    {['Administrador', 'Gerente', 'Atendente'].includes(profile) && order.status === 'ProntoParaRetirada' && <Box component="form" onSubmit={event => { event.preventDefault(); void act(`/api/ordens-servico/${order.id}/entrega`, { recebedor: recipient, garantias: services.map(servico => ({ servico, dias: Number(warranties[servico]?.dias ?? 90), condicoes: warranties[servico]?.condicoes ?? 'Garantia do serviço realizado.' })) }, 'Entregue'); }}><Stack spacing={1}>
      <Typography>Entrega e garantia</Typography><TextField label="Pessoa que recebeu" required value={recipient} onChange={event => setRecipient(event.target.value)} />
      {services.map((servico, index) => <Stack key={`${servico}-${index}`} spacing={1}><Typography>{servico}</Typography>
        <TextField label="Dias de garantia" type="number" inputProps={{ min: 1, max: 3650 }} required value={warranties[servico]?.dias ?? '90'} onChange={event => setWarranties({ ...warranties, [servico]: { dias: event.target.value, condicoes: warranties[servico]?.condicoes ?? '' } })} />
        <TextField label="Condições de garantia" required value={warranties[servico]?.condicoes ?? ''} onChange={event => setWarranties({ ...warranties, [servico]: { dias: warranties[servico]?.dias ?? '90', condicoes: event.target.value } })} />
      </Stack>)}
      <Button type="submit" variant="contained" disabled={payments === null || payments.saldo > 0}>Registrar entrega</Button>
    </Stack></Box>}
    {detail && detail.reparos.length > 0 && <Stack spacing={0.5}><Typography variant="h6">Reparos</Typography>{detail.reparos.map(repair => <Typography key={repair.id}>{new Date(repair.inicio).toLocaleString('pt-BR')} · {repair.servicosRealizados ?? 'Em andamento'}{repair.observacoes ? ` · ${repair.observacoes}` : ''}</Typography>)}</Stack>}
    {detail && <>
      <Typography variant="h6">Inspeção de entrada</Typography>
      {order.atendimentoDireto ? <Stack spacing={1.5}>{detail.pontosAvaria.map((point, index) => <Box key={index}><Typography><strong>{damageLabel(point)}</strong>{pointDefects(point).length > 0 ? ` — ${pointDefects(point).join('; ')}` : ''}</Typography>{point.observacao && <Typography color="text.secondary" sx={{ fontSize: 13, whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{point.observacao}</Typography>}</Box>)}<Typography color="text.secondary" sx={{ fontSize: 12 }}>As seis faces e a localização dos defeitos constam no relatório para assinatura.</Typography></Stack> : detail.checklist.map(x => <Typography key={x.id}>{x.itemNome}: {x.resposta}</Typography>)}
      {detail.testes.map(test => <Stack key={test.id} spacing={0.5}><Typography variant="h6">Teste {new Date(test.createdAt).toLocaleString('pt-BR')} · {test.aprovado ? 'Aprovado' : 'Reprovado'}</Typography><Typography>{test.resultado}</Typography>{detail.respostasTeste.filter(x => x.testeFinalId === test.id).map(x => <Typography key={x.id}>{x.itemNome}: {x.resultado}</Typography>)}</Stack>)}
      <Typography variant="h6">Histórico</Typography>{detail.timeline.map(x => <Typography key={x.id}>{new Date(x.createdAt).toLocaleString('pt-BR')} · {x.evento}{x.detalhes ? ` · ${x.detalhes}` : ''}</Typography>)}
    </>}
  </Stack></Paper>;
}
