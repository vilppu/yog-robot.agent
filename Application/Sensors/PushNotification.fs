namespace YogRobot

module internal PushNotification =
    
    type Subscription =
        { Token : string }

    let Subscription token : Subscription =
        { Token = token }
  