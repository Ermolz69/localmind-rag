import {
  Controller,
  Post,
  Body,
  Req,
  UseGuards,
  Get,
  Res,
  UnauthorizedException,
} from '@nestjs/common';
import { AuthService } from './auth.service';
import { KeysService } from './keys.service';
import { JwtAuthGuard } from './jwt-auth.guard';
import type { Request, Response } from 'express';

@Controller()
export class AuthController {
  constructor(
    private readonly authService: AuthService,
    private readonly keysService: KeysService,
  ) {}

  @Post('auth/register')
  async register(
    @Body()
    body: {
      email: string;
      password?: string;
      deviceName?: string;
      platform?: string;
    },
  ) {
    const { email, password } = body;
    return this.authService.register(email, password);
  }

  @Post('auth/login')
  async login(
    @Body()
    body: {
      email: string;
      password?: string;
      deviceName?: string;
      platform?: string;
    },
    @Req() req: Request,
  ) {
    const { email, password } = body;
    const user = await this.authService.validateUser(email, password || '');
    if (!user) throw new UnauthorizedException('Invalid credentials');

    const deviceMetadata = {
      userAgent: req.headers['user-agent'],
      ipAddress: req.ip,
      deviceName: body.deviceName,
      platform: body.platform,
    };

    return this.authService.login(user, deviceMetadata);
  }

  @Post('auth/refresh')
  async refresh(@Body() body: { sessionId: string; refreshToken: string }) {
    return this.authService.refresh(body.sessionId, body.refreshToken);
  }

  @UseGuards(JwtAuthGuard)
  @Post('auth/logout')
  async logout(@Req() req: Request & { user?: { sessionId: string } }) {
    // req.user is populated by JwtStrategy
    await this.authService.logout(req.user?.sessionId || '');
    return { success: true };
  }

  @UseGuards(JwtAuthGuard)
  @Post('auth/logout-all')
  async logoutAll(@Req() req: Request & { user?: { userId: string } }) {
    await this.authService.logoutAll(req.user?.userId || '');
    return { success: true };
  }

  @Get('.well-known/jwks.json')
  getJwks(@Res() res: Response) {
    const pubKey = this.keysService.getPublicKey();
    // Normally JWKS is a specific JSON format, but providing the PEM as a simple JWK or just sending the public key depending on SyncApi implementation.
    // To keep it simple, we expose it as JSON with the key.
    // Real JWKS uses modulo (n) and exponent (e), but we'll return a minimal custom JWKS that the .NET API can parse as RS256 PEM.
    return res.json({
      keys: [
        {
          kty: 'RSA',
          alg: 'RS256',
          use: 'sig',
          kid: 'localmind-auth-key-1',
          x5c: [Buffer.from(pubKey).toString('base64')],
        },
      ],
    });
  }
}
