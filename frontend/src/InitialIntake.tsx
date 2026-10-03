import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Alert, Box, Button, Chip, MenuItem, Paper, Stack, Step, StepButton, Stepper, TextField, Typography } from '@mui/material';
import { request, type Aparelho, type Cliente, type Orcamento, type Ordem, type TipoReparo, type Unidade } from './api';
import { DeviceDamage3D, type DamagePoint } from './DeviceDamage3D';
import { Icon } from './ui';
import type { Service } from './ServiceAdministration';
import { IntakeIdentity } from './IntakeIdentity';
import { IntakeBudget, itemTotal, type Piece, type BudgetDraft } from './IntakeBudget';
import './intake.css';

export type InitialResult = { ordem: Ordem; orcamento: Orcamento; linkAprovacao: string; relatorioUrl: string; email: { status: string; mensagem: string } };
const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
const steps = ['Cliente e aparelho', 'Problema e inspeção', 'Orçamento'];
const commonProblems = ['Tela quebrada', 'Conector', 'Bateria estufada', 'Vida útil de bateria', 'Face ID', 'Câmera', 'Traseira'];

export function InitialIntake({ token, units, clients, devices, repairTypes, onRefresh, onCreated }: {
  token: string; units: Unidade[]; clients: Cliente[]; devices: Aparelho[]; repairTypes: TipoReparo[];
  onRefresh: () => Promise<void>; onCreated: (result: InitialResult) => void;
}) {
  const [unitId, setUnitId] = useState('');
  const [client, setClient] = useState<Cliente | null>(null); const [device, setDevice] = useState<Aparelho | null>(null);
  const [step, setStep] = useState(0); const heading = useRef<HTMLDivElement>(null);
  const [problems, setProblems] = useState<string[]>([]);
  const [defect, setDefect] = useState(''); const [notes, setNotes] = useState(''); const [points, setPoints] = useState<DamagePoint[]>([]);
  const [selectedTypes, setSelectedTypes] = useState<string[]>([]);
  const [services, setServices] = useState<Service[]>([]); const [pieces, setPieces] = useState<Piece[]>([]);
  const [budget, setBudget] = useState<BudgetDraft>({ items: [], discount: '0', validDays: '7', deadline: '', terms: '' });
  const { items, discount, validDays, deadline, terms } = budget;
  const [error, setError] = useState(''); const [saving, setSaving] = useState(false);
  const subtotal = items.reduce((sum, item) => sum + itemTotal(item), 0);
  const total = subtotal - Number(discount || 0);
  const reportedProblem = [...problems, defect.trim()].filter(Boolean).join('; ');

  useEffect(() => { if (!unitId && units[0]) setUnitId(units[0].id); }, [units, unitId]);
  useEffect(() => { request<Service[]>('/api/servicos', token).then(setServices).catch(() => {}); request<Piece[]>('/api/pecas', token).then(setPieces).catch(() => {}); }, [token]);

  function goToStep(next: number) {
    setStep(next); setError('');
    heading.current?.scrollIntoView({ block: 'start' }); heading.current?.focus({ preventScroll: true });
  }
  function validateStep(index: number) {
    if (index === 0 && (!unitId || !client || !device || device.clienteId !== client.id)) return 'Selecione a unidade, o cliente e o aparelho para continuar.';
    if (index === 1 && !reportedProblem) return 'Selecione um problema frequente ou descreva o relato do cliente.';
    if (index === 1 && reportedProblem.length > 4000) return 'O relato deve ter no máximo 4.000 caracteres.';
    if (index === 1 && !points.length) return 'Registre ao menos um defeito constatado na inspeção do aparelho.';
    if (index === 2 && (!items.length || !Number.isFinite(Number(discount)) || Number(discount) < 0 || total < 0 || !deadline.trim())) return 'Inclua o orçamento inicial e confira desconto e prazo.';
    if (index === 2 && (!Number.isInteger(Number(validDays)) || Number(validDays) < 1 || Number(validDays) > 365)) return 'A validade deve ser de 1 a 365 dias.';
    return '';
  }
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (saving) return;
    for (let index = 0; index <= step; index++) {
      const issue = validateStep(index);
      if (issue) { goToStep(index); setError(issue); return; }
    }
    if (step < steps.length - 1) { goToStep(step + 1); return; }
    setSaving(true);
    try {
      const result = await request<InitialResult>('/api/ordens-servico/atendimento-inicial', token, 'POST', {
        unidadeId: unitId, clienteId: client!.id, aparelhoId: device!.id, defeitoRelatado: reportedProblem, observacoes: notes || null,
        prioridade: 'Normal', pontosAvaria: points, tiposReparo: selectedTypes,
        orcamento: { itens: items, desconto: Number(discount), validoAte: new Date(Date.now() + Number(validDays) * 86400000).toISOString(), prazoEstimado: deadline, condicoes: terms || null },
      });
      onCreated(result); void onRefresh().catch(() => {});
    } catch (error) { setError(String(error)); }
    finally { setSaving(false); }
  }

  return <Box sx={{ maxWidth: 1100 }}>
    <Paper className="intake-steps" ref={heading} tabIndex={-1} sx={{ mb: 2.5, scrollMarginTop: 20, outline: 'none' }}>
      <Stepper activeStep={step} alternativeLabel>{steps.map((label, index) => <Step key={label} completed={index < step}><StepButton type="button" disabled={index > step || saving} onClick={() => goToStep(index)} aria-current={index === step ? 'step' : undefined}>{label}</StepButton></Step>)}</Stepper>
    </Paper>
    {step > 0 && client && device && <Stack direction="row" flexWrap="wrap" alignItems="center" gap={1} sx={{ mb: 2 }}><Chip icon={<Icon name="users" size={15}/>} label={client.nome} variant="outlined"/><Chip icon={<Icon name="devices" size={15}/>} label={`${device.marca} ${device.modelo}`} variant="outlined"/><Button size="small" onClick={() => goToStep(0)} disabled={saving}>Alterar cadastro</Button></Stack>}
    {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
    <Box component="form" onSubmit={submit} sx={{ display: 'grid', gap: 2.5 }}>
      <Box component="fieldset" hidden={step !== 0} disabled={step !== 0 || saving} sx={{ border: 0, m: 0, p: 0, minWidth: 0 }}>
        <Paper sx={{ p: { xs: 2, sm: 3 } }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} gap={2} justifyContent="space-between" sx={{ mb: 3 }}><Box><Typography variant="h6">Cliente e aparelho</Typography><Typography color="text.secondary" sx={{ fontSize: 13, mt: .5 }}>Encontre um cadastro ou adicione os dois de uma só vez.</Typography></Box><TextField select size="small" label="Unidade de atendimento" value={unitId} onChange={e => setUnitId(e.target.value)} required sx={{ minWidth: 210 }}>{units.map(x => <MenuItem key={x.id} value={x.id}>{x.nome}</MenuItem>)}</TextField></Stack>
          <IntakeIdentity token={token} clients={clients} devices={devices} client={client} device={device} onClient={value => { if (value?.id !== client?.id) setDevice(null); setClient(value); setError(''); }} onDevice={value => { setDevice(value); setError(''); }}/>
        </Paper>
      </Box>
      <Box component="fieldset" hidden={step !== 1} disabled={step !== 1 || saving} sx={{ border: 0, m: 0, p: 0, minWidth: 0 }}>
        <Paper sx={{ p: { xs: 2, sm: 3 } }}>
          <Typography variant="h6">Problema e inspeção</Typography>
          <Typography color="text.secondary" sx={{ fontSize: 13, mt: .5, mb: 2.5 }}>Selecione o que o cliente relata e registre os defeitos constatados no aparelho.</Typography>
          <Box sx={{ p: 2, bgcolor: '#f1f8f6', border: '1px solid #dcebe6', borderRadius: 2.5, mb: 2.5 }}>
            <Typography fontWeight={750} fontSize={13} sx={{ mb: 1.3 }}>Problemas mais comuns</Typography>
            <Stack direction="row" flexWrap="wrap" gap={1} aria-label="Problemas mais comuns">{commonProblems.map(problem => <Chip component="button" type="button" key={problem} clickable label={problem} color={problems.includes(problem) ? 'primary' : 'default'} variant={problems.includes(problem) ? 'filled' : 'outlined'} icon={<Icon name={problems.includes(problem) ? 'check' : 'plus'} size={15}/>} aria-pressed={problems.includes(problem)} onClick={() => setProblems(current => current.includes(problem) ? current.filter(item => item !== problem) : [...current, problem])} sx={{ bgcolor: problems.includes(problem) ? undefined : '#fff' }}/>)}</Stack>
            <Typography color="text.secondary" fontSize={12} sx={{ mt: 1.3 }}>Pode selecionar mais de um. Os problemas escolhidos serão incluídos no relato da OS.</Typography>
          </Box>
          <TextField label={problems.length ? 'Detalhes do relato (opcional)' : 'Problema relatado pelo cliente'} placeholder="Ex.: caiu no chão, tela sem imagem; começou ontem…" value={defect} onChange={e => setDefect(e.target.value)} fullWidth multiline minRows={2} inputProps={{ maxLength: 4000 }} sx={{ mb: 2 }}/>
          {reportedProblem && <Box sx={{ borderLeft: '3px solid #83b9ae', pl: 1.5, mb: 2.5 }}><Typography fontSize={10} fontWeight={800} color="text.secondary">RELATO QUE VAI PARA A OS</Typography><Typography fontSize={13} sx={{ mt: .5, overflowWrap: 'anywhere' }}>{reportedProblem}</Typography></Box>}
          <DeviceDamage3D points={points} onChange={value => { setPoints(value); setError(''); }}/>
          <TextField label="Observações do atendimento (opcional)" placeholder="Ex.: acessórios entregues e outras condições de entrada" value={notes} onChange={e => setNotes(e.target.value)} fullWidth multiline minRows={2} inputProps={{ maxLength: 4000 }} sx={{ mt: 2.5 }}/>
          {repairTypes.length > 0 && <Box sx={{ mt: 2.5 }}><Typography sx={{ fontWeight: 700, mb: 1 }}>Tipos de reparo previstos</Typography><Stack direction="row" flexWrap="wrap" gap={1}>{repairTypes.map(type => <Button key={type.id} type="button" variant={selectedTypes.includes(type.id) ? 'contained' : 'outlined'} size="small" onClick={() => setSelectedTypes(selectedTypes.includes(type.id) ? selectedTypes.filter(id => id !== type.id) : [...selectedTypes, type.id])}>{type.nome}</Button>)}</Stack></Box>}
        </Paper>
      </Box>
      <Box component="fieldset" hidden={step !== 2} disabled={step !== 2 || saving} sx={{ border: 0, m: 0, p: 0, minWidth: 0 }}>
        <IntakeBudget value={budget} onChange={value => { setBudget(value); setError(''); }} services={services} pieces={pieces}/>
      </Box>
      <Box className="intake-footer">
        <Box><Typography fontWeight={750} fontSize={13}>Etapa {step + 1} de 3</Typography><Typography color="text.secondary" fontSize={12}>{step === 2 ? `${items.length} ${items.length === 1 ? 'item' : 'itens'} · ${money(Math.max(0, total))}` : 'Você pode voltar sem perder o preenchimento.'}</Typography></Box>
        <div className="intake-footer-actions">{step > 0 && <Button type="button" variant="outlined" onClick={() => goToStep(step - 1)} disabled={saving}>Voltar</Button>}<Button type="submit" variant="contained" disabled={saving} endIcon={!saving && step < 2 ? <Icon name="arrow" size={17}/> : undefined}>{saving ? 'Abrindo OS…' : step === 0 ? 'Continuar para inspeção' : step === 1 ? 'Continuar para orçamento' : 'Abrir OS e apresentar orçamento'}</Button></div>
      </Box>
    </Box>
  </Box>;
}
