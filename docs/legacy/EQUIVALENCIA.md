# IT Digitação Legacy — checklist de equivalência

Base de auditoria: Standard em `d8962ed`; Legacy completa iniciada em `legacy/net35-full`.

| Área da Standard | Recursos encontrados | Estado Legacy |
|---|---|---|
| Início | duração 5–30 min, boas-vindas, mascote, início de sessão | Parcial na beta.2: banner em duas colunas, seleção em cards e benefícios; revisar responsividade e fidelidade visual |
| Biblioteca | catálogo `content/atividades.json`, categorias e ilustrações | Parcial na beta.2: 10 cards ilustrados, contagens, busca, filtros e atividades; revisar estados e comportamento de retomada |
| Prática | editor, comparação caractere a caractere, progresso, cronômetro, precisão, velocidade | A implementar |
| Resultados | resumo e métricas da sessão | A implementar |
| Conquistas | regras em `SessionAwards` e apresentação visual | A implementar |
| Guia ABNT2 | mapa de teclas, dedos e exercício guiado | A implementar |
| Configurações | fonte, som, métricas, concentração, feedback e efeitos | A implementar |
| Persistência | preferências JSON no perfil do usuário | A implementar com serializer disponível no .NET 3.5 |
| Atalho | não existe na Standard | Novo requisito Legacy |
| Offline | assets e catálogo locais | Obrigatório |

## Fontes da auditoria

- Telas: `src/ITDigitacao.App/Views/{Home,Library,Practice,Results,Guide,Settings,Question}View.xaml`.
- Navegação e ciclo de vida: `src/ITDigitacao.App/MainWindow.xaml(.cs)`.
- Regras: `src/ITDigitacao.Core/{ActivityRepository,TypingEngine,SessionManager,SessionAwards,Preferences}.cs`.
- Recursos: `src/ITDigitacao.App/Assets/` e `content/atividades.json`.

Esta tabela é um checklist de implementação, não uma alegação de equivalência concluída. Cada item deve ser atualizado após implementação e teste. Home e Biblioteca foram parcialmente migradas na beta.2 e ainda precisam de comparação visual/teste presencial.

## Auditoria de compatibilidade da base Standard

A inspeção confirmou que o código compartilhado não pode ser simplesmente ligado ao projeto .NET 3.5. A base usa recursos da linguagem e da biblioteca posteriores ao framework alvo, incluindo `IReadOnlyList<T>`, `Array.Empty<T>`, interpolação de strings, filtros `catch when`, `out var`, `nameof`, membros com corpo de expressão e operadores `?.`/`??`. Alguns podem ser aceitos pelo compilador moderno quando direcionado ao framework antigo, mas dependências de BCL como `IReadOnlyList<T>` e `Array.Empty<T>` não existem no .NET 3.5 e precisam ser removidas/substituídas no código Legacy.

O catálogo e as preferências usam `DataContractJsonSerializer` (disponível na família .NET 3.5 com referência a `System.Runtime.Serialization`), enquanto a interface Standard depende de XAML/BAML e serviços WPF adicionais. A prova de conceito confirma apenas que o WPF sem XAML inicia no equipamento validado; ela não demonstra a compatibilidade das telas completas.

### Migração necessária

- Criar projetos Legacy separados para UI, regras e utilitário de atalhos, todos direcionados a v3.5/x86.
- Adaptar o Core para APIs BCL 3.5 e manter as regras sem alterações semânticas.
- Implementar as telas via construção programática de controles WPF, reaproveitando texto, imagens e catálogo locais.
- Manter testes de equivalência para a biblioteca e o motor; comparar contagem e IDs do catálogo com a Standard.
- Compilar em Windows Actions e testar a inicialização e fluxos no Windows 7 SP1 real antes de declarar compatibilidade.
