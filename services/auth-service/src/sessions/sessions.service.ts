import { Injectable } from '@nestjs/common';
import { PrismaService } from '../prisma/prisma.service';
import { EventsService } from '../events/events.service';
import * as bcrypt from 'bcrypt';

@Injectable()
export class SessionsService {
  constructor(
    private readonly prisma: PrismaService,
    private readonly events: EventsService,
  ) {}

  async createSession(userId: string, deviceId: string, refreshToken: string) {
    const refreshTokenHash = await bcrypt.hash(refreshToken, 10);
    // Refresh tokens valid for 30 days
    const expiresAt = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000);

    const session = await this.prisma.session.create({
      data: {
        userId,
        deviceId,
        refreshTokenHash,
        expiresAt,
      },
    });

    this.events.emitEvent('auth.session.created', { sessionId: session.id, userId, deviceId });
    return session;
  }

  async validateSession(sessionId: string, plainRefreshToken: string) {
    const session = await this.prisma.session.findUnique({ where: { id: sessionId } });
    if (!session || session.expiresAt < new Date()) {
      return null;
    }
    
    const isValid = await bcrypt.compare(plainRefreshToken, session.refreshTokenHash);
    if (!isValid) {
      return null;
    }
    
    return session;
  }

  async revokeSession(sessionId: string) {
    const session = await this.prisma.session.delete({ where: { id: sessionId } });
    this.events.emitEvent('auth.session.revoked', { sessionId: session.id, userId: session.userId });
    return session;
  }

  async revokeAllUserSessions(userId: string) {
    await this.prisma.session.deleteMany({ where: { userId } });
    this.events.emitEvent('auth.sessions.revoked_all', { userId });
  }
}
