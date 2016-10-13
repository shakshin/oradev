# C+ Oracle Developer
IDE for PL/SQL code development

Requirements:
.Net Framework v4.0

Main functionality:
- Multiple encodings support for open and save operations
- Syntax highlighting (powered by AvalonEdit)
- Standart set of tex-editing tools: Undo, Redo, Search, Replace etc.
- Rectangular block selection
- Monitor external changes in open files: file was changes, deleted or renamed by another application
- Single instance mode: all files opened in one window

- Execute SQL queries and show result in table view
- Explain plan of queries
- Copy results of query in justified table format
- Execute PL/SQL scripts
- Compile PL/SQL packages, show erros if any and navigate to corresponding lines

- Navigate through source code elements (procedures, functions, cursors)
- Navigate through DB objects (packages, tables) and open sources for editing

- Almost all database operations are implemented as separate thread tasks to  avoid IDE blocks while long-time database operations performing
- Multiple database connections supported
- IDE does not hold permanent database connections. Connections opened when it's required and closed when operation s complete

- KILLER FEATURE!!! IDE checks if any session is locking package you going to compile, shows you list of such sesions and allow you to kill this session(s)


