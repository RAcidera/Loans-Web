// Domain layer — mirrors the Users table in the SRS.

import { UserRole } from '../../application/auth/auth.service';

export type UserStatus = 'active' | 'inactive';

export interface AppUser {
  userId: string;
  username: string;
  role: UserRole;
  status: UserStatus;
  createdAt: string;
}
