import { Injectable, OnModuleInit, Logger } from '@nestjs/common';
import * as fs from 'fs';
import * as path from 'path';

@Injectable()
export class KeysService implements OnModuleInit {
  private readonly logger = new Logger(KeysService.name);
  private publicKey: string;
  private privateKey: string;

  onModuleInit() {
    const certDir = path.join(process.cwd(), 'certs');
    const pubKeyPath = path.join(certDir, 'public.pem');
    const privKeyPath = path.join(certDir, 'private.pem');

    try {
      this.publicKey = fs.readFileSync(pubKeyPath, 'utf8');
      this.privateKey = fs.readFileSync(privKeyPath, 'utf8');
      this.logger.log('RS256 keys loaded successfully.');
    } catch (error) {
      this.logger.error('Failed to load RS256 keys. Run node generate-keys.js first.', error);
      throw error;
    }
  }

  getPublicKey(): string {
    return this.publicKey;
  }

  getPrivateKey(): string {
    return this.privateKey;
  }
}
