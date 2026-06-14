# Sample Report Guidance

Use sample reports carefully. SignalScan reports can contain machine names, usernames, device labels, timestamps, and technician-entered client details.

## Safe Sample Creation

- Use a non-client machine or VM when possible.
- Use fake client names, phone numbers, email addresses, and device labels.
- Review the PDF and Markdown outputs before sharing.
- Remove personal identifiers before publishing or sending a sample.
- Do not publish reports with real computer names, usernames, serial numbers, client contact details, or private business information.
- Keep sample reports outside the repository unless they are intentionally sanitized.

## Repository Rule

Do not commit generated client reports containing real personal or client data.

If a sanitized sample is later added, place it under:

```text
docs/demo-kit/samples
```

The sample filename and the report content must clearly include `SAMPLE`, and the report must use fake/sample client information.
