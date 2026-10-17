# Icarus Platform

Contém a API, o processamento assíncrono, as regras de negócio e a persistência
dos dados transacionais do Icarus.

## Acesso local pelo aplicativo

Para testar em Android físico ou iPhone na mesma rede, inicie a API com o perfil
de desenvolvimento `rede-local`:

```bash
dotnet run --project src/Icarus.Api --launch-profile rede-local
```

Configure `EXPO_PUBLIC_API_URL=http://IP_DA_MAQUINA:5138` no `icarus-mobile`.
O perfil `http` padrão continua ligado apenas a `localhost`. Use o perfil
`rede-local` somente em uma rede de desenvolvimento confiável; o ambiente
publicado deverá expor a API por HTTPS pelo proxy.
