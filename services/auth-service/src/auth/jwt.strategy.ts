import { ExtractJwt, Strategy } from 'passport-jwt';
import { PassportStrategy } from '@nestjs/passport';
import { Injectable } from '@nestjs/common';
import { KeysService } from './keys.service';

@Injectable()
export class JwtStrategy extends PassportStrategy(Strategy) {
  constructor(private readonly keysService: KeysService) {
    super({
      jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),
      ignoreExpiration: false,
      secretOrKey: keysService.getPublicKey(),
      algorithms: ['RS256'],
    });
  }

  async validate(payload: any) {
    return { userId: payload.sub, email: payload.email, role: payload.role, sessionId: payload.sessionId };
  }
}
