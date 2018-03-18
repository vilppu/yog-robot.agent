namespace YogRobot

module PushNotification =
    
    type Subscription =
        { Token : string }

    let Subscription token : Subscription =
        { Token = token }
  