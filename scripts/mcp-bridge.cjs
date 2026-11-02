#!/usr/bin/env node
const readline = require('readline');

// Support configuration via CLI args or environment variables
const targetUrl = process.argv[2] || process.env.MCP_SERVER_URL || 'https://localhost:7013/mcp';
const rawAuth = process.env.AUTHORIZATION || process.argv[3] || '';
const authHeader = rawAuth ? (rawAuth.startsWith('Bearer ') ? rawAuth : `Bearer ${rawAuth}`) : '';

// Suppress warnings on stderr so MCP JSON-RPC protocol is completely clean
process.env.NODE_NO_WARNINGS = '1';
// Allow self-signed development certificates for localhost
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  terminal: false
});

rl.on('line', async (line) => {
  const trimmed = line.trim();
  if (!trimmed) return;

  try {
    const payload = JSON.parse(trimmed);

    // Standard MCP initialize response
    if (payload.method === 'initialize') {
      const initResult = {
        jsonrpc: '2.0',
        id: payload.id,
        result: {
          protocolVersion: '2024-11-05',
          capabilities: {
            tools: {}
          },
          serverInfo: {
            name: 'enjoyeveryday',
            version: '1.0.0'
          }
        }
      };
      process.stdout.write(JSON.stringify(initResult) + '\n');
      return;
    }

    if (payload.method === 'notifications/initialized') {
      // No response needed for notification
      return;
    }

    const headers = {
      'Content-Type': 'application/json'
    };
    if (authHeader) {
      headers['Authorization'] = authHeader;
    }

    const res = await fetch(targetUrl, {
      method: 'POST',
      headers,
      body: JSON.stringify(payload)
    });

    const data = await res.json();
    process.stdout.write(JSON.stringify(data) + '\n');
  } catch (err) {
    const errResponse = {
      jsonrpc: '2.0',
      id: null,
      error: {
        code: -32603,
        message: err.message || 'Internal MCP proxy error'
      }
    };
    process.stdout.write(JSON.stringify(errResponse) + '\n');
  }
});
