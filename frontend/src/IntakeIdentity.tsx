import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Autocomplete, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Divider, InputAdornment, Stack, TextField, Typography } from '@mui/material';
import { request, type Aparelho, type Cliente, type Lista } from './api';
import { Icon } from './ui';

const brands = ['Apple', 'Samsung', 'Motorola', 'Xiaomi', 'Realme', 'OPPO', 'ASUS', 'LG'];
const columns = { display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 };

export function IntakeIdentity({ token, clients, devices, client, device, onClient, onDevice }: {
  token: string; clients: Cliente[]; devices: Aparelho[]; client: Cliente | null; device: Aparelho | null;
  onClient: (value: Cliente | null) => void; onDevice: (value: Aparelho | null) => void;
}) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<Cliente[]>(clients);
  const [localClients, setLocalClients] = useState<Cliente[]>([]);
  const [deviceOptions, setDeviceOptions] = useState<Aparelho[]>([]);
  const [localDevices, setLocalDevices] = useState<Aparelho[]>([]);
  const [loading, setLoading] = useState(false); const [loadingDevices, setLoadingDevices] = useState(false);
  const [searchError, setSearchError] = useState('');
  const [modal, setModal] = useState<'new' | 'device' | 'email' | null>(null);
  const [busy, setBusy] = useState(false); const [error, setError] = useState('');
  const [name, setName] = useState(''); const [phone, setPhone] = useState(''); const [email, setEmail] = useState('');
  const [brand, setBrand] = useState(''); const [model, setModel] = useState(''); const [imei, setImei] = useState('');
  const [createdClient, setCreatedClient] = useState<Cliente | null>(null);

  useEffect(() => {
    let active = true;
    const term = query.trim();
    setSearchError('');
    if (!term) { setResults(clients); setLoading(false); return; }
    setLoading(true);
    const timer = window.setTimeout(() => {
      request<Lista<Cliente>>(`/api/clientes?busca=${encodeURIComponent(term)}&tamanho=50`, token)
        .then(data => { if (active) setResults(data.itens); })
        .catch(() => { if (active) setSearchError('Não foi possível buscar clientes. Tente novamente.'); })
        .finally(() => { if (active) setLoading(false); });
    }, 220);
    return () => { active = false; window.clearTimeout(timer); };
  }, [query, clients, token]);

  useEffect(() => {
    let active = true;
    setDeviceOptions(devices.filter(item => item.clienteId === client?.id));
    if (!client) { setLoadingDevices(false); return; }
    setLoadingDevices(true);
    request<Aparelho[]>(`/api/aparelhos?clienteId=${client.id}`, token)
      .then(data => { if (active) setDeviceOptions(data); })
      .catch(() => { if (active) setSearchError('Não foi possível carregar os aparelhos. Selecione o cliente novamente para tentar.'); })
      .finally(() => { if (active) setLoadingDevices(false); });
    return () => { active = false; };
  }, [client, devices, token]);

  const term = query.toLocaleLowerCase('pt-BR').trim();
  const options = [...(client ? [client] : []), ...localClients, ...results]
    .filter((item, index, list) => list.findIndex(other => other.id === item.id) === index)
    .filter(item => !term || `${item.nome} ${item.telefone}`.toLocaleLowerCase('pt-BR').includes(term));
  const availableDevices = [...localDevices, ...deviceOptions]
    .filter((item, index, list) => item.clienteId === client?.id && list.findIndex(other => other.id === item.id) === index);

  function open(kind: 'new' | 'device' | 'email') {
    setError(''); setCreatedClient(null);
    setName(kind === 'new' && !client && !/^\+?[\d\s()-]+$/.test(query) ? query.trim() : '');
    setPhone(kind === 'new' && /^\+?[\d\s()-]+$/.test(query) ? query.trim() : '');
    setEmail(kind === 'email' ? client?.email ?? '' : '');
    setBrand(''); setModel(''); setImei(''); setModal(kind);
  }
  function close() { if (!busy) { setModal(null); setError(''); } }
  function rememberClient(value: Cliente) {
    setLocalClients(current => [value, ...current.filter(item => item.id !== value.id)]);
    onClient(value); setQuery(value.nome);
  }
  async function save(event: FormEvent) {
    event.preventDefault(); event.stopPropagation();
    if (busy) return;
    setBusy(true); setError('');
    try {
      if (modal === 'email' && client) {
        // The update endpoint replaces optional fields: retain the complete existing record.
        const existing = await request<Cliente>(`/api/clientes/${client.id}`, token);
        const updated = await request<Cliente>(`/api/clientes/${client.id}`, token, 'PUT', { ...existing, email: email.trim() });
        rememberClient(updated); setModal(null); return;
      }
      let owner = modal === 'new' ? createdClient : client;
      if (!owner) {
        owner = await request<Cliente>('/api/clientes', token, 'POST', { nome: name.trim(), telefone: phone.trim(), email: email.trim() || null });
        setCreatedClient(owner); rememberClient(owner);
      }
      const registered = await request<Aparelho>('/api/aparelhos', token, 'POST', { clienteId: owner.id, marca: brand.trim(), modelo: model.trim(), imei: imei.trim() || null });
      setLocalDevices(current => [registered, ...current]); onDevice(registered); setModal(null);
    } catch (failure) { setError(failure instanceof Error ? failure.message : 'Não foi possível salvar o cadastro.'); }
    finally { setBusy(false); }
  }

  return <>
    {searchError && <Alert severity="warning" sx={{ mb: 2 }}>{searchError}</Alert>}
    <Box sx={columns}>
      <Box className="intake-identity-card">
        <Stack direction="row" spacing={1.2} alignItems="center" sx={{ mb: 2 }}><span className="intake-card-icon"><Icon name="users"/></span><Box><Typography fontWeight={750}>Quem está trazendo?</Typography><Typography variant="body2" color="text.secondary">Busque pelo nome ou telefone.</Typography></Box></Stack>
        <Autocomplete options={options} value={client} inputValue={query} loading={loading} loadingText="Buscando clientes…" noOptionsText="Cliente não encontrado. Use o cadastro abaixo." filterOptions={items => items}
          getOptionLabel={item => item.nome} isOptionEqualToValue={(a, b) => a.id === b.id}
          onInputChange={(_, value) => setQuery(value)} onChange={(_, value) => onClient(value)}
          renderOption={(props, option) => { const { key, ...rest } = props; return <li key={key} {...rest}><Box><Typography fontSize={14} fontWeight={650}>{option.nome}</Typography><Typography fontSize={12} color="text.secondary">{option.telefone}{option.email ? ` · ${option.email}` : ''}</Typography></Box></li>; }}
          renderInput={params => <TextField {...params} label="Buscar cliente" placeholder="Nome ou telefone" InputProps={{ ...params.InputProps, startAdornment: <InputAdornment position="start"><Icon name="search" size={17}/></InputAdornment> }}/>} />
        {client ? <Box sx={{ mt: 2 }}><Chip size="small" color="success" variant="outlined" icon={<Icon name="check" size={15}/>} label="Cliente selecionado"/><Typography fontWeight={750} sx={{ mt: 1 }}>{client.nome}</Typography><Typography variant="body2" color="text.secondary">{client.telefone}</Typography><Typography variant="body2" color="text.secondary" sx={{ overflowWrap: 'anywhere' }}>{client.email || 'E-mail não informado'}</Typography>{!client.email && <Button size="small" onClick={() => open('email')}>Adicionar e-mail para receber a OS</Button>}</Box> : <Box sx={{ py: 2 }}><Typography variant="body2" color="text.secondary">Primeiro atendimento? Cadastre o cliente e o aparelho juntos.</Typography></Box>}
        <Button type="button" onClick={() => open('new')} startIcon={<Icon name="plus" size={16}/>} sx={{ mt: 1 }}>Novo cliente e aparelho</Button>
      </Box>
      <Box className="intake-identity-card" data-disabled={!client}>
        <Stack direction="row" spacing={1.2} alignItems="center" sx={{ mb: 2 }}><span className="intake-card-icon"><Icon name="devices"/></span><Box><Typography fontWeight={750}>Qual é o aparelho?</Typography><Typography variant="body2" color="text.secondary">Dispositivos vinculados ao cliente.</Typography></Box></Stack>
        {!client ? <div className="intake-device-empty"><Icon name="devices" size={30}/><Typography variant="body2">Selecione um cliente ou faça um novo cadastro.</Typography></div> : <>
          <Autocomplete options={availableDevices} value={device} disabled={loadingDevices} loading={loadingDevices} loadingText="Carregando aparelhos…" noOptionsText="Nenhum aparelho encontrado. Cadastre abaixo."
            getOptionLabel={item => `${item.marca} ${item.modelo}${item.imei ? ` · ${item.imei}` : ''}`} isOptionEqualToValue={(a, b) => a.id === b.id}
            onChange={(_, value) => onDevice(value)} renderInput={params => <TextField {...params} label="Selecionar aparelho"/>}/>
          {device ? <Box sx={{ mt: 2 }}><Chip size="small" color="success" variant="outlined" icon={<Icon name="check" size={15}/>} label="Aparelho selecionado"/><Typography fontWeight={750} sx={{ mt: 1 }}>{device.marca} {device.modelo}</Typography><Typography variant="body2" color="text.secondary">{device.imei ? `IMEI ${device.imei}` : 'Sem IMEI informado'}</Typography></Box> : <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>{loadingDevices ? 'Consultando aparelhos…' : availableDevices.length ? 'Escolha o aparelho que será atendido.' : 'Este cliente ainda não tem aparelhos cadastrados.'}</Typography>}
          <Button type="button" onClick={() => open('device')} startIcon={<Icon name="plus" size={16}/>} sx={{ mt: 1 }}>Novo aparelho para este cliente</Button>
        </>}
      </Box>
    </Box>
    <Dialog open={modal !== null} onClose={close} fullWidth maxWidth={modal === 'email' ? 'xs' : 'sm'}>
      <Box component="form" onSubmit={save}>
        <DialogTitle>{modal === 'new' ? 'Novo cliente e aparelho' : modal === 'device' ? 'Novo aparelho' : 'E-mail do cliente'}<Typography color="text.secondary" variant="body2" sx={{ mt: .5 }}>{modal === 'new' ? 'Um único cadastro para começar o atendimento.' : `Vinculado a ${client?.nome ?? ''}`}</Typography></DialogTitle>
        <DialogContent>
          <Box component="fieldset" disabled={busy} sx={{ border: 0, m: 0, p: 0, minWidth: 0 }}>
            {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
            {createdClient && <Alert severity="info" sx={{ mb: 2 }}>Cliente já salvo. Complete o aparelho para concluir o cadastro.</Alert>}
            {modal === 'new' && <><Typography className="intake-form-label">DADOS DO CLIENTE</Typography><Box sx={columns}>
              <TextField label="Nome completo" value={name} onChange={e => setName(e.target.value)} required disabled={!!createdClient} autoFocus autoComplete="name" inputProps={{ maxLength: 160 }} sx={{ gridColumn: { sm: 'span 2' } }}/>
              <TextField label="Telefone / WhatsApp" value={phone} onChange={e => setPhone(e.target.value)} required disabled={!!createdClient} type="tel" autoComplete="tel" placeholder="(11) 99999-9999" inputProps={{ maxLength: 32 }}/>
              <TextField label="E-mail (opcional)" value={email} onChange={e => setEmail(e.target.value)} type="email" autoComplete="email" disabled={!!createdClient} inputProps={{ maxLength: 254 }} helperText="Para enviar a cópia da OS."/>
            </Box><Divider sx={{ my: 3 }}/></>}
            {modal !== 'email' ? <><Typography className="intake-form-label">DADOS DO APARELHO</Typography><Stack direction="row" gap={.7} flexWrap="wrap" sx={{ mb: 2 }} aria-label="Marcas frequentes">{brands.slice(0, 4).map(item => <Chip component="button" type="button" key={item} clickable size="small" label={item} variant={brand === item ? 'filled' : 'outlined'} color={brand === item ? 'primary' : 'default'} aria-pressed={brand === item} onClick={() => setBrand(item)}/>)}</Stack><Box sx={columns}>
              <Autocomplete freeSolo options={brands} inputValue={brand} onInputChange={(_, value) => setBrand(value)} renderInput={params => <TextField {...params} label="Marca" required inputProps={{ ...params.inputProps, maxLength: 80 }}/>}/>
              <TextField label="Modelo" value={model} onChange={e => setModel(e.target.value)} required autoFocus={modal === 'device'} placeholder="Ex.: iPhone 13" inputProps={{ maxLength: 120 }}/>
              <TextField label="IMEI (opcional)" value={imei} onChange={e => setImei(e.target.value)} inputProps={{ maxLength: 32 }} helperText="Pode ser preenchido depois, se indisponível na entrada." sx={{ gridColumn: { sm: 'span 2' } }}/>
            </Box></> : <TextField label="E-mail para receber a OS" value={email} onChange={e => setEmail(e.target.value)} type="email" required fullWidth autoFocus inputProps={{ maxLength: 254 }} sx={{ mt: 1 }}/>}
          </Box>
        </DialogContent>
        <DialogActions sx={{ p: 2.5 }}><Button onClick={close} disabled={busy}>Cancelar</Button><Button type="submit" variant="contained" disabled={busy} startIcon={busy ? <CircularProgress size={16} color="inherit"/> : <Icon name="check" size={16}/>}>{busy ? 'Salvando…' : modal === 'email' ? 'Salvar e-mail' : 'Cadastrar e usar na OS'}</Button></DialogActions>
      </Box>
    </Dialog>
  </>;
}
