import { Module } from '@nestjs/common';
import { AuthService } from './auth.service';
import { AuthController } from './auth.controller';
import { JwtModule } from '@nestjs/jwt';
import { PassportModule } from '@nestjs/passport';
import { KeysService } from './keys.service';
import { JwtStrategy } from './jwt.strategy';
import { UsersModule } from '../users/users.module';
import { SessionsModule } from '../sessions/sessions.module';
import { DevicesModule } from '../devices/devices.module';

@Module({
  imports: [
    UsersModule,
    SessionsModule,
    DevicesModule,
    PassportModule,
    JwtModule.registerAsync({
      inject: [KeysService],
      useFactory: (keysService: KeysService) => ({
        privateKey: keysService.getPrivateKey(),
        publicKey: keysService.getPublicKey(),
        signOptions: {
          algorithm: 'RS256',
          expiresIn: '15m',
        },
      }),
    }),
  ],
  providers: [AuthService, KeysService, JwtStrategy],
  controllers: [AuthController],
  exports: [KeysService], // Export KeysService for JwtModule.registerAsync
})
export class AuthModule {}
