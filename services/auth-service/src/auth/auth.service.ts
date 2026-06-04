import { Injectable, UnauthorizedException } from '@nestjs/common';
import { UsersService } from '../users/users.service';
import { JwtService } from '@nestjs/jwt';
import * as bcrypt from 'bcrypt';
import { SessionsService } from '../sessions/sessions.service';
import { DevicesService } from '../devices/devices.service';
import { randomBytes } from 'crypto';

@Injectable()
export class AuthService {
  constructor(
    private readonly usersService: UsersService,
    private readonly jwtService: JwtService,
    private readonly sessionsService: SessionsService,
    private readonly devicesService: DevicesService,
  ) {}

  async validateUser(email: string, pass: string): Promise<any> {
    const user = await this.usersService.findByEmail(email);
    if (user && await bcrypt.compare(pass, user.passwordHash)) {
      const { passwordHash, ...result } = user;
      return result;
    }
    return null;
  }

  async login(user: any, deviceMetadata: any) {
    const device = await this.devicesService.trackDevice(user.id, deviceMetadata);
    
    // Generate an opaque refresh token
    const refreshToken = randomBytes(64).toString('hex');
    const session = await this.sessionsService.createSession(user.id, device.id, refreshToken);

    const payload = { email: user.email, sub: user.id, role: user.role, sessionId: session.id };
    return {
      accessToken: this.jwtService.sign(payload),
      refreshToken,
    };
  }

  async register(email: string, pass: string) {
    const passwordHash = await bcrypt.hash(pass, 10);
    return this.usersService.createUser(email, passwordHash);
  }

  async refresh(sessionId: string, refreshToken: string) {
    const session = await this.sessionsService.validateSession(sessionId, refreshToken);
    if (!session) {
      throw new UnauthorizedException('Invalid or expired refresh token');
    }

    const user = await this.usersService.findById(session.userId);
    if (!user) throw new UnauthorizedException('User not found');

    const payload = { email: user.email, sub: user.id, role: user.role, sessionId: session.id };
    return {
      accessToken: this.jwtService.sign(payload),
    };
  }

  async logout(sessionId: string) {
    await this.sessionsService.revokeSession(sessionId);
  }

  async logoutAll(userId: string) {
    await this.sessionsService.revokeAllUserSessions(userId);
  }
}
