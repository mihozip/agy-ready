# Security scope

Checks are read only. Repair writes only `HKCU\Environment\Path`, preserves its raw value and registry type, and keeps a local backup for conditional restore. The installer runs only after confirmation and uses fixed official HTTPS script URLs. No credential access, machine PATH write, elevation, or execution-policy change is present. `ProcessRunner` uses `UseShellExecute = false`, `ArgumentList`, bounded output, and timeouts. Raw installer output is retained only in memory and is not shown or shared; report redaction remains Milestone 5.

The clasp action runs the fixed `npm install @google/clasp -g` command after explicit confirmation. It does not run `clasp login`, read Google credentials, or display raw npm output. A successful npm exit code alone is not treated as a successful clasp setup.
