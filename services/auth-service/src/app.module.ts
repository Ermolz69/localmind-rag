import { Module } from '@nestjs/common';
import { PrismaModule } from './prisma/prisma.module';
import { EventsModule } from './events/events.module';
import { UsersModule } from './users/users.module';
import { SessionsModule } from './sessions/sessions.module';
import { DevicesModule } from './devices/devices.module';
import { AuthModule } from './auth/auth.module';

@Module({
  imports: [
    PrismaModule,
    EventsModule,
    UsersModule,
    SessionsModule,
    DevicesModule,
    AuthModule,
  ],
})
export class AppModule {}
