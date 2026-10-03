export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  shopName: string
  city?: string
  phoneNumber?: string
}

/** The password was right and a code from the authenticator app is needed next. */
export interface TwoFactorChallenge {
  requiresTwoFactor: true
  challenge: string
  message: string
}

export interface AuthResult {
  message: string
  tokens: {
    accessToken: string
    /** Empty: the refresh token travels in an HttpOnly cookie. */
    refreshToken?: string
    expiresIn: number
    tokenType: string
  }
}
